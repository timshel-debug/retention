# Dependency Inversion Refactor — Requirements Specification (Retention)

## 1) Scope and intent

**Objective:** Apply the **Dependency Inversion Principle (DIP)** across the Retention solution so that:

* **Application/Domain code does not create its own dependency graph** (no “newing up” service dependencies).
* All **service dependencies are expressed as abstractions** (interfaces) and are **wired in a composition root at startup** (ASP.NET Core `Program.cs` via `IServiceCollection`).
* The codebase becomes easier to test, reason about, and evolve without “concrete coupling drift”.

**In-scope projects**

* `src/Retention.Api` (composition root + HTTP boundary)
* `src/Retention.Application` (orchestration + mapping + validation + evaluation pipeline)
* `src/Retention.Domain` (policy evaluation + ranking/selection strategies)

**Out of scope**

* Cosmetic refactors not related to dependency inversion (naming, formatting).
* Re-architecting the domain model or changing external API contracts (unless required to preserve DIP constraints).

---

## 2) Definitions (normative)

* **Composition Root:** The executable entrypoint where the object graph is composed (ASP.NET Core: `Retention.Api/Program.cs`). Only the composition root may “choose” implementations.
* **Service dependency:** A collaborator that provides behaviour and could vary by environment or policy (e.g., evaluator, builder, specification, mapper, telemetry).
* **Acceptable `new`:**

  * Value objects / records / DTOs / results
  * Collections
  * Pure algorithmic helpers with no external coupling
  * **Within the composition root only** for wiring (factories/decorators)

---

## 3) Current concrete-coupling inventory (must be eliminated in production code)

### 3.1 Composition root creates a concrete service instance

* `src/Retention.Api/Program.cs` (line ~47): `builder.Services.AddSingleton(new EvaluateRetentionService());`

### 3.2 API layer depends on a concrete Application type

* `src/Retention.Api/Services/RetentionEvaluatorAdapter.cs` (line ~14): constructor takes `EvaluateRetentionService` instead of `IEvaluateRetentionService`

### 3.3 Application service builds its own dependency graph (parameterless constructor)

* `src/Retention.Application/EvaluateRetentionService.cs` (line ~31): `EvaluateRetentionService()` calls `new RetentionPolicyEvaluator(...)`, `new DefaultGroupRetentionEvaluator()`, `new TelemetryGroupRetentionEvaluator(...)`

### 3.4 Application engine/factory builds pipeline with concrete instances

* `src/Retention.Application/Evaluation/RetentionEvaluationEngine.cs` (line ~41): `CreateDefault(...)` constructs steps/builders/specs/mappers via `new ...`

### 3.5 Static factory creates a “hard-coded” rule chain

* `src/Retention.Application/Validation/ValidationRuleChainFactory.cs` (line ~7): creates rule list directly via `new ...`

### 3.6 Domain services create other domain services (parameterless constructors)

* `src/Retention.Domain/Services/RetentionPolicyEvaluator.cs` (line ~20): `RetentionPolicyEvaluator()` calls `new DefaultGroupRetentionEvaluator()`
* `src/Retention.Domain/Services/GroupRetentionEvaluator.cs` (line ~21): `DefaultGroupRetentionEvaluator()` calls `new DefaultRankingStrategy()`, `new TopNSelectionStrategy()`

### 3.7 Tests/benchmarks currently rely on removed constructors (must be updated)

* Benchmarks/tests instantiate `new RetentionPolicyEvaluator()` and `new EvaluateRetentionService(...)` in multiple files.

---

## 4) Requirements

### DIP-REQ-0001 — Single composition root owns all wiring

**Statement:** The complete service graph for Retention evaluation must be registered via `IServiceCollection` at startup, with **no production code path** constructing its own service dependencies outside the composition root.

**Acceptance criteria**

* Running `dotnet test` passes without any service needing a parameterless constructor to function.
* No production constructors (Domain/Application) call `new` for other services/builders/mappers/specs/evaluators.

---

### DIP-REQ-0002 — Remove parameterless constructors that construct dependencies

**Statement:** Any constructor that exists only to build a default dependency graph must be removed or made non-production (e.g., `internal` for tests only, but preferred: remove entirely).

**Targets**

* `EvaluateRetentionService()` in `Retention.Application`
* `RetentionPolicyEvaluator()` in `Retention.Domain`
* `DefaultGroupRetentionEvaluator()` in `Retention.Domain`

**Acceptance criteria**

* The only public constructors on service types require abstractions as parameters.
* No production code compiles that calls these removed parameterless constructors.

---

### DIP-REQ-0003 — API layer depends only on abstractions

**Statement:** `Retention.Api` must not depend on concrete Application services (except trivial DTOs). Controllers/adapters must depend on interfaces.

**Targets**

* `RetentionEvaluatorAdapter` must depend on `IEvaluateRetentionService` (not `EvaluateRetentionService`).

**Acceptance criteria**

* `RetentionEvaluatorAdapter` constructor parameter is `IEvaluateRetentionService`.
* DI can resolve all controllers/services without referencing concrete `EvaluateRetentionService` types at injection sites.

---

### DIP-REQ-0004 — Replace `CreateDefault` / internal composition with DI-resolved components

**Statement:** `RetentionEvaluationEngine` must not expose a “default builder” (`CreateDefault`) that constructs concrete dependencies. The engine must be created with dependencies provided by DI.

**Required design shape (one of)**

* **Preferred:** `IRetentionEvaluationEngine` registered in DI; it takes an ordered list of steps and executes them.
* **Acceptable:** `RetentionEvaluationEngine` built via a DI factory in the composition root (or extension method) that constructs the step list using DI-resolved dependencies (the only location allowed to `new` steps).

**Acceptance criteria**

* No `new ReferenceIndexBuilder()`, `new DefaultDeploymentValiditySpecification()`, `new DecisionLogAssembler()` etc. inside `RetentionEvaluationEngine` in production code.
* Pipeline step ordering remains deterministic and explicit.

---

### DIP-REQ-0005 — Validation rules are registered and composed via DI

**Statement:** The validation “chain” must be formed from DI registrations, not from a hard-coded static factory.

**Constraints**

* Rule order must be deterministic **without relying on DI registration order** (explicit ordering required).

**Acceptance criteria**

* `ValidationRuleChainFactory.CreateDefaultChain()` is removed or replaced with DI-based composition.
* There is a deterministic ordering mechanism (e.g., `Order` property, or a separate ordered wrapper type).

---

### DIP-REQ-0006 — Domain evaluation graph uses injected strategies/evaluators

**Statement:** Domain services must not construct other domain services internally.

**Targets**

* `RetentionPolicyEvaluator` must require an `IGroupRetentionEvaluator` (already exists—remove default `new` path).
* `DefaultGroupRetentionEvaluator` must require `IRetentionRankingStrategy` and `IRetentionSelectionStrategy` (already exists—remove default `new` path).

**Acceptance criteria**

* No parameterless constructors in `Retention.Domain/Services/*` create service dependencies.
* Domain services can be instantiated entirely by DI registration.

---

### DIP-REQ-0007 — Startup registration is expressed as standard .NET service registration

**Statement:** All production dependencies must be registered using standard `Microsoft.Extensions.DependencyInjection` patterns.

**Required structure**

* Introduce **one or more** extension methods:

  * `Retention.Application`: `IServiceCollection AddRetentionApplication(this IServiceCollection services)`
  * `Retention.Domain`: `IServiceCollection AddRetentionDomain(this IServiceCollection services)`
* `Retention.Api/Program.cs` calls these extension methods and only contains top-level host wiring.

**Acceptance criteria**

* `Program.cs` contains only composition-root concerns (host, auth, controllers, swagger, the `AddRetention*` calls).
* All retention evaluation services resolve from `ServiceProvider` created by `Program`.

---

### DIP-REQ-0008 — Explicit service lifetimes

**Statement:** Registrations must define appropriate lifetimes based on statefulness.

**Baseline lifetime rules**

* **Singleton:** pure/stateless services (strategies, builders, specifications, mappers, assemblers, evaluators if stateless)
* **Scoped:** request-bound boundary services (rare here unless per-request context is introduced)
* **Transient:** only if the service holds per-call state (avoid if not necessary)

**Acceptance criteria**

* No “accidental singleton with mutable state” is introduced.
* Services that are pure remain singleton to avoid churn.

---

### DIP-REQ-0009 — Telemetry abstraction (optional but recommended for “full DIP”)

**Statement:** Replace direct static telemetry calls with an injectable abstraction where telemetry is used as a collaborator.

**Targets**

* `RetentionTelemetry` (static) usage in `EvaluateRetentionService` and `TelemetryGroupRetentionEvaluator`

**Acceptance criteria**

* Application services depend on an interface (e.g., `IRetentionTelemetry`) rather than static calls.
* Production wiring provides the concrete telemetry implementation; tests can provide a no-op.

---

### DIP-REQ-0010 — API ActivitySource usage is injectable (optional)

**Statement:** API services (`DatasetValidatorService`, `RetentionEvaluatorAdapter`) must not own static `ActivitySource`.

**Acceptance criteria**

* `ActivitySource` (or equivalent abstraction) is injected and registered in the composition root.

---

## 5) Registration map (minimum required)

This is the minimum set of collaborators that must be DI-managed (names based on current code):

### Domain

* `IRetentionPolicyEvaluator` → `RetentionPolicyEvaluator`
* `IGroupRetentionEvaluator` → `TelemetryGroupRetentionEvaluator` (decorator)

  * inner concrete: `DefaultGroupRetentionEvaluator`
* `IRetentionRankingStrategy` → `DefaultRankingStrategy`
* `IRetentionSelectionStrategy` → `TopNSelectionStrategy`

### Application

* `IEvaluateRetentionService` → `EvaluateRetentionService`
* `IRetentionEvaluationEngine` (new) → `RetentionEvaluationEngine`
* `IReferenceIndexBuilder` → `ReferenceIndexBuilder`
* `IDeploymentValiditySpecification` → `DefaultDeploymentValiditySpecification`
* `IKeptReleaseMapper` → `KeptReleaseMapper`
* `IDecisionLogAssembler` → `DecisionLogAssembler`
* `IDiagnosticsCalculator` → `DiagnosticsCalculator`
* Validation rules: register each concrete rule as `IValidationRule` (plus ordering metadata)

### API

* `IRetentionEvaluator` → `RetentionEvaluatorAdapter`
* `IDatasetValidator` → `DatasetValidatorService`

---

## 6) Enforcement requirements (to prevent regression)

### DIP-NFR-0001 — Architecture tests / static enforcement

**Statement:** Add an automated check to prevent re-introducing concrete coupling.

**Minimum enforcement**

* A test that scans production assemblies for forbidden patterns, e.g.:

  * “No `new` of types matching `*Evaluator`, `*Service`, `*Builder`, `*Mapper`, `*Assembler`, `*Specification`, `*Step` inside `Retention.Domain` or `Retention.Application`”
  * “No references from Domain → Api”
  * “No constructors without parameters on service types”

**Acceptance criteria**

* CI fails if a new DIP violation is introduced.

### DIP-NFR-0002 — No external contract changes

**Statement:** HTTP endpoints, request/response payloads, and validation semantics must remain unchanged.

**Acceptance criteria**

* Existing unit + integration + golden snapshot tests continue to pass.
* No controller route or contract changes.

---

## 7) Test requirements (must be added/updated)

### DIP-TEST-0001 — DI composition test

**Statement:** Add a test that builds the full service provider (via `AddRetentionDomain` + `AddRetentionApplication`) and resolves:

* `IEvaluateRetentionService`
* `IRetentionEvaluator` (API adapter)
* Any pipeline engine type

Then run a minimal evaluation with representative inputs to ensure the graph is complete.

**Acceptance criteria**

* Test fails if any required dependency is missing from DI registration.

### DIP-TEST-0002 — Update benchmarks and unit tests to use DI or explicit constructors

**Statement:** Any benchmark/test that relied on removed parameterless constructors must be updated.

**Acceptance criteria**

* `Retention.Benchmarks` compiles and runs.
* Unit tests no longer call removed default constructors.

---

## 8) Deliverable checklist

To consider DIP “done”, the following must be true:

* No production code (Domain/Application) constructs service dependencies internally.
* All service wiring is in the composition root (directly or via `AddRetention*` extension methods).
* Step/rule ordering is deterministic by explicit mechanism.
* Unit + integration + golden tests pass.
* DI composition test exists and passes.
* An enforcement mechanism exists to prevent regressions.

---
