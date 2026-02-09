## Definition of Done (DIP across this repo)

* **All runtime composition happens in the composition root** (DI registration at startup), not inside application/domain service constructors.
* **No “service graph wiring” via `new`** inside `Retention.Application` or `Retention.Domain` (value objects/DTOs excluded).
* **Application + domain services are resolved via interfaces** where they are consumed (especially from `Retention.Api`).
* `dotnet test` passes for **UnitTests + Api.IntegrationTests**; benchmarks still build.

---

## Slice plan (implementation-ready)

### SLICE-DIP-000 — Baseline inventory + rules-of-the-road (docs only)

**Goal:** Make the refactor explicit and deterministic.

**Model:** gpt-5 mini — fast, precise doc/update work.

```text
TASK (SLICE-DIP-000)
- Add docs/di-refactor/00_DipRefactorPlan.md documenting:
  - What counts as a “service” in this repo (Application/Domain classes registered in DI)
  - What is exempt (records/entities/DTOs/result objects)
  - The rule: no default ctors that construct collaborators; no “new XService()” in Application/Domain.
  - The composition roots: Retention.Api Program.cs + Application DI extension(s)
  - A short inventory of current offenders:
    - Retention.Application/EvaluateRetentionService.cs (default ctor + engine creation)
    - Retention.Application/Evaluation/RetentionEvaluationEngine.cs (static Create + internal wiring)
    - Retention.Application/Validation/ValidationRuleChainFactory.cs (static factory builds rules via new)
    - Retention.Domain/Services/RetentionPolicyEvaluator.cs (parameterless ctor)
    - Retention.Domain/Services/GroupRetentionEvaluator.cs (parameterless ctor)
- No functional changes.

ACCEPTANCE
- Repo builds.
- No tests need changing.

COMMANDS
- dotnet test tests/Retention.UnitTests/Retention.UnitTests.csproj
```

---

### SLICE-DIP-001 — Introduce centralized DI registration (no behaviour change yet)

**Goal:** One place to register services; Program.cs becomes thin.

**Model:** claude-sonnet — multi-file refactor with careful wiring.

```text
TASK (SLICE-DIP-001)
Create DI extension(s) and move existing registrations into them WITHOUT changing behaviour.

FILES
- Add: src/Retention.Application/DependencyInjection/ServiceCollectionExtensions.cs
- Add: src/Retention.Domain/DependencyInjection/ServiceCollectionExtensions.cs
- Update: src/Retention.Api/Program.cs

STEPS
1) In Retention.Domain, add:
   - public static IServiceCollection AddRetentionDomain(this IServiceCollection services)
   - (For now) only register the domain services already used at runtime OR leave empty but present.

2) In Retention.Application, add:
   - public static IServiceCollection AddRetentionApplication(this IServiceCollection services)
   - Call services.AddRetentionDomain()
   - Register the same concrete types Program.cs registers today (EvaluateRetentionService, RetentionEvaluationEngine, etc.) so behaviour is unchanged in this slice.

3) Update Retention.Api/Program.cs to call builder.Services.AddRetentionApplication(); and remove the duplicated registrations from Program.cs.

CONSTRAINTS
- Do not remove any constructors yet.
- Do not change lifetimes unless required to preserve behaviour.

ACCEPTANCE
- Unit + integration tests pass unchanged.

COMMANDS
- dotnet test tests/Retention.UnitTests/Retention.UnitTests.csproj
- dotnet test tests/Retention.Api.IntegrationTests/Retention.Api.IntegrationTests.csproj
```

---

### SLICE-DIP-002 — API consumes abstractions (stop depending on concrete EvaluateRetentionService)

**Goal:** `Retention.Api` depends on interfaces, not concrete application services.

**Model:** claude-sonnet — interface-first refactor with small surface area.

```text
TASK (SLICE-DIP-002)
Refactor Retention.Api to depend on IEvaluateRetentionService rather than EvaluateRetentionService.

FILES
- Update: src/Retention.Api/Services/RetentionEvaluatorAdapter.cs
- Update: src/Retention.Application/IEvaluateRetentionService.cs (if needed)
- Update: src/Retention.Application/EvaluateRetentionService.cs (ctor signature if needed)
- Update: src/Retention.Application/DependencyInjection/ServiceCollectionExtensions.cs

STEPS
1) Change RetentionEvaluatorAdapter ctor to take IEvaluateRetentionService (not EvaluateRetentionService).
2) Ensure EvaluateRetentionService implements IEvaluateRetentionService (already true in this repo; keep it that way).
3) In DI registration, register:
   - services.AddScoped<IEvaluateRetentionService, EvaluateRetentionService>();
   - (Do not register EvaluateRetentionService as the type consumers take)
4) Ensure no controllers/services in Retention.Api request EvaluateRetentionService directly.

ACCEPTANCE
- No project in Retention.Api references EvaluateRetentionService concretely.
- All tests pass.

COMMANDS
- dotnet test tests/Retention.UnitTests/Retention.UnitTests.csproj
- dotnet test tests/Retention.Api.IntegrationTests/Retention.Api.IntegrationTests.csproj
```

---

### SLICE-DIP-003 — Domain services: remove parameterless constructors that “wire the graph”

**Goal:** `Retention.Domain` services only accept dependencies via ctor.

**Model:** claude-sonnet — cross-test updates + safe constructor changes.

```text
TASK (SLICE-DIP-003)
Remove parameterless constructors from domain services and update tests accordingly.

FILES
- Update: src/Retention.Domain/Services/RetentionPolicyEvaluator.cs
- Update: src/Retention.Domain/Services/GroupRetentionEvaluator.cs
- Update tests that call:
  - new RetentionPolicyEvaluator()
  - new GroupRetentionEvaluator()

STEPS
1) Remove/obsolete parameterless ctor from RetentionPolicyEvaluator.
   - Keep only ctor that takes IGroupRetentionEvaluator.
2) Remove/obsolete parameterless ctor from GroupRetentionEvaluator.
   - Keep only ctor that takes IRetentionRankingStrategy + IRetentionSelectionStrategy.
3) Update failing tests:
   - Prefer building a ServiceProvider using AddRetentionApplication() and resolving the needed service,
     OR explicitly construct using concrete defaults (DefaultRankingStrategy, TopNSelectionStrategy, DefaultGroupRetentionEvaluator, etc.).
   - Keep changes minimal; do not rewrite unrelated tests.

ACCEPTANCE
- No parameterless ctors remain on those two types.
- All tests pass.

COMMANDS
- dotnet test tests/Retention.UnitTests/Retention.UnitTests.csproj
- dotnet test tests/Retention.Api.IntegrationTests/Retention.Api.IntegrationTests.csproj
```

---

### SLICE-DIP-004 — Validation chain: move rule wiring to DI (kill static factory wiring)

**Goal:** Validation rules are composed at startup, not via `ValidationRuleChainFactory` constructing them ad-hoc.

**Model:** claude-sonnet — ordering-sensitive refactor.

```text
TASK (SLICE-DIP-004)
Stop using ValidationRuleChainFactory for building rule lists. Wire the ordered list via DI registration.

FILES
- Update: src/Retention.Application/Validation/ValidationRuleChainFactory.cs (deprecate or delete if unused)
- Update: src/Retention.Application/Evaluation/Steps/ValidateInputsStep.cs (if needed)
- Update: src/Retention.Application/DependencyInjection/ServiceCollectionExtensions.cs
- Update tests that assume factory behaviour (if any)

STEPS
1) In AddRetentionApplication(), register an ORDERED IReadOnlyList<IValidationRule> matching the current factory order.
   - Use a singleton factory: services.AddSingleton<IReadOnlyList<IValidationRule>>(sp => new IValidationRule[] { ... });
   - Ensure NonNegativeReleasesToKeepRule is included in the correct position.
2) Ensure ValidateInputsStep receives that list exactly (constructor already takes IReadOnlyList<IValidationRule>).
3) Remove runtime usage of ValidationRuleChainFactory (leave file only if referenced by docs; otherwise delete).

ACCEPTANCE
- No production code calls ValidationRuleChainFactory to build the rules.
- Rule order is deterministic and matches previous behaviour.
- Tests pass.

COMMANDS
- dotnet test tests/Retention.UnitTests/Retention.UnitTests.csproj
- dotnet test tests/Retention.Api.IntegrationTests/Retention.Api.IntegrationTests.csproj
```

---

### SLICE-DIP-005 — Evaluation engine: DI supplies ordered pipeline steps (remove `Create()` wiring)

**Goal:** `RetentionEvaluationEngine` becomes a pure executor over injected steps; step ordering is composed at startup.

**Model:** claude-opus — safest for a structural refactor with ordering constraints.

```text
TASK (SLICE-DIP-005)
Refactor RetentionEvaluationEngine so it does not "new up" steps or collaborators.

FILES
- Update: src/Retention.Application/Evaluation/RetentionEvaluationEngine.cs
- Update: src/Retention.Application/DependencyInjection/ServiceCollectionExtensions.cs
- Update: src/Retention.Application/EvaluateRetentionService.cs (temporarily adapt)
- Update tests/benchmarks impacted

STEPS
1) In RetentionEvaluationEngine:
   - Remove static Create(...) and any default/parameterless wiring.
   - Constructor should take IReadOnlyList<IEvaluationStep> steps (or IEnumerable with materialization).
   - Engine.Run executes steps in the provided order.

2) In DI registration:
   - Register each concrete step as singleton:
     - ValidateInputsStep
     - BuildReferenceIndexStep
     - FilterInvalidDeploymentsStep
     - EvaluatePolicyStep
     - MapResultsStep
     - BuildDecisionLogStep
     - FinalizeResultStep
   - Register an ORDERED IReadOnlyList<IEvaluationStep> that resolves those steps in the exact order above.
     (Do NOT rely on “enumerable registration order” implicitly; build the list explicitly.)

3) Ensure engine is registered and can be resolved.

ACCEPTANCE
- No engine code constructs steps or factories.
- Pipeline order is explicit in DI registration.
- Tests pass.

COMMANDS
- dotnet test tests/Retention.UnitTests/Retention.UnitTests.csproj
- dotnet test tests/Retention.Api.IntegrationTests/Retention.Api.IntegrationTests.csproj
```

---

### SLICE-DIP-006 — EvaluateRetentionService: remove default ctor + stop composing engine internally

**Goal:** No more service-graph creation in application services.

**Model:** claude-sonnet — targeted refactor with test updates.

```text
TASK (SLICE-DIP-006)
Make EvaluateRetentionService fully DI-driven.

FILES
- Update: src/Retention.Application/EvaluateRetentionService.cs
- Update: src/Retention.Application/DependencyInjection/ServiceCollectionExtensions.cs
- Update tests that instantiate EvaluateRetentionService directly

STEPS
1) Remove parameterless/default ctor from EvaluateRetentionService.
2) Replace current ctor dependencies so it no longer calls RetentionEvaluationEngine.Create(...).
   - Prefer ctor(EetentionEvaluationEngine engine) (or interface) and use it directly.
3) Ensure DI registers:
   - IEvaluateRetentionService -> EvaluateRetentionService
   - RetentionEvaluationEngine as singleton (or scoped if you prefer; keep stateless)
4) Update unit tests:
   - Prefer resolving IEvaluateRetentionService from a ServiceProvider built with AddRetentionApplication().
   - Keep behavioural assertions identical.

ACCEPTANCE
- No EvaluateRetentionService ctor creates other services/engines/policies via new.
- All tests pass.

COMMANDS
- dotnet test tests/Retention.UnitTests/Retention.UnitTests.csproj
- dotnet test tests/Retention.Api.IntegrationTests/Retention.Api.IntegrationTests.csproj
```

---

### SLICE-DIP-007 — Benchmarks project uses the same DI composition

**Goal:** No “hand-wired” graph in benchmarks that drifts from production.

**Model:** gpt-5 mini — mechanical updates with limited scope.

```text
TASK (SLICE-DIP-007)
Update benchmarks to build a ServiceProvider using AddRetentionApplication() and resolve required services.

FILES
- Update: benchmarks/Retention.Benchmarks/* (as needed)
- Possibly update: benchmarks/Retention.Benchmarks/Program.cs or benchmark setup

STEPS
- Replace direct construction of application/domain services with DI resolution.
- Ensure benchmark still measures the same code paths.

ACCEPTANCE
- benchmarks project builds.
- No production service graph wiring exists inside benchmarks (only DI composition).

COMMAND
- dotnet build benchmarks/Retention.Benchmarks/Retention.Benchmarks.csproj
```

---

### SLICE-DIP-008 — Guardrails: architecture test to prevent regression

**Goal:** Stop future re-introduction of “new-ing” service graphs.

**Model:** claude-sonnet — reflection-based guardrails done carefully.

```text
TASK (SLICE-DIP-008)
Add an architecture unit test enforcing DIP rules for service types.

FILES
- Add: tests/Retention.UnitTests/Architecture/DependencyInversionTests.cs (new)
- Possibly add a small helper to build ServiceProvider via AddRetentionApplication()

TEST IDEAS (implement at least these)
1) Reflection: assert these types DO NOT have public parameterless ctors:
   - Retention.Application.EvaluateRetentionService
   - Retention.Application.Evaluation.RetentionEvaluationEngine
   - Retention.Domain.Services.RetentionPolicyEvaluator
   - Retention.Domain.Services.GroupRetentionEvaluator

2) DI smoke: build ServiceProvider with AddRetentionApplication(), then resolve:
   - IEvaluateRetentionService
   - IRetentionEvaluator (API adapter not available in unit tests; only if referenced)
   - RetentionEvaluationEngine
   - IReadOnlyList<IEvaluationStep>
   - IReadOnlyList<IValidationRule>

ACCEPTANCE
- Test fails if a parameterless ctor is reintroduced.
- Unit tests pass.

COMMAND
- dotnet test tests/Retention.UnitTests/Retention.UnitTests.csproj
```

---

### SLICE-DIP-009 — Cleanup: remove dead wiring + make Program.cs purely composition root

**Goal:** Remove obsolete factories/constructors and reduce drift risk.

**Model:** gpt-5 mini — safe cleanup with strict scope.

```text
TASK (SLICE-DIP-009)
Remove deprecated code paths that were kept during the refactor.

FILES (conditional; only if unused now)
- Delete or simplify: src/Retention.Application/Validation/ValidationRuleChainFactory.cs
- Remove unused registrations for concrete types that should only be consumed via interfaces.
- Ensure Program.cs contains only:
  - framework wiring (controllers, swagger, etc.)
  - builder.Services.AddRetentionApplication();
  - builder.Services.AddScoped<IRetentionEvaluator, RetentionEvaluatorAdapter>(); etc.

ACCEPTANCE
- No unused code paths remain for old composition.
- Tests pass.

COMMANDS
- dotnet test tests/Retention.UnitTests/Retention.UnitTests.csproj
- dotnet test tests/Retention.Api.IntegrationTests/Retention.Api.IntegrationTests.csproj
```

---
