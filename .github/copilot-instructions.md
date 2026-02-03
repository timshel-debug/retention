# Copilot Instructions: Release Retention Solution

## Project Overview
Multi-layered solution for **Release Retention** evaluation with three components:
1. **Library** (.NET 8): Core retention logic (Domain + Application layers) - library-first, embeddable
2. **REST API** (ASP.NET Core): HTTP wrapper with validation, auth, rate limiting, Swagger
3. **UI** (React + TypeScript + Vite): Interactive console for dataset validation and retention evaluation

**Architecture**: Clean Architecture with strict layer separation. See [docs/retention_application/01_Overview.md](../docs/retention_application/01_Overview.md).

## Architecture: Clean Architecture (3 Layers)

Strict layer separation per [ADR-0001](../docs/adr/ADR-0001-greenfield-clean-architecture.md):

### Retention.Domain (Pure Business Rules)
- **No dependencies** - not even logging or telemetry
- **Core service**: `RetentionPolicyEvaluator` - implements ranking, eligibility, tie-breakers
- **Entities**: `Project`, `Environment`, `Release`, `Deployment` (immutable records)
- **Pattern**: Accepts pre-validated lookups (dictionaries), returns `ReleaseCandidate` list
- Example: [RetentionPolicyEvaluator.cs](../src/Retention.Domain/Services/RetentionPolicyEvaluator.cs)

### Retention.Application (Orchestration + DTOs)
- **Service**: `EvaluateRetentionService` - validates input, handles invalid references, returns DTOs
- **DTOs**: `RetentionResult`, `KeptRelease`, `DecisionLogEntry`, `RetentionDiagnostics`
- **Error handling**: Throws `ValidationException` (input errors) or `DomainException` (invariants)
- **Telemetry**: Uses `System.Diagnostics` ActivitySource/Meter (no OTel package dependency)
- Example: [EvaluateRetentionService.cs](../src/Retention.Application/EvaluateRetentionService.cs)

### Critical Principle: Decision Logs as Data, Not Side-Effects
Per [ADR-0004](../docs/adr/ADR-0004-decision-logging.md), domain layer **returns** decision entries as structured data. Logging happens only at host boundary (if needed). Never log inside domain/application logic during evaluation.

## Core Business Logic

### Primary Rule (REQ-0003)
> For each **(ProjectId, EnvironmentId)** combination, keep `n` releases **most recently deployed** to that environment.

**Most recently deployed** = `max(DeployedAt)` across all deployments of a release to that environment (REQ-0004).

### Deterministic Ranking Tie-Breakers (ADR-0003)
When releases have equal latest deployment timestamps within a project/environment:
1. `LatestDeployedAt` desc *(primary ranking)*
2. `Release.Created` desc *(tie-breaker 1)*
3. `Release.Id` asc *(tie-breaker 2 - ordinal/alphabetical)*

**Why**: Ensures deterministic test outputs and satisfies NFR-0003.

### Eligibility Rule (REQ-0002)
Release is eligible for an environment **only if** it has ≥1 deployment to that environment. Zero deployments = not eligible.

### Multi-Environment Evaluation (REQ-0006)
Single `EvaluateRetention()` call processes **all** project/environment combinations. A release can appear in results multiple times (kept for different environments). Union of kept releases across all (project, env) groups is returned.

### Invalid Reference Handling (ADR-0005)
Deployments referencing missing projects/environments/releases:
- **Excluded** from eligibility (silently skipped in evaluation)
- **Logged** as diagnostic entries with `diagnostic.invalid_reference` reason code
- **Never throw** - evaluation continues for valid data
- See [ADR-0005](../docs/adr/ADR-0005-invalid-references.md)

## Development Workflows

### Full Stack Development
```powershell
# Start API + UI together (recommended for integrated development)
.\start-dev.ps1           # HTTP mode: API on http://localhost:5219, UI on http://localhost:5173
.\start-dev.ps1 -https    # HTTPS mode: API on https://localhost:7080

# Script handles:
# - Building .NET solution
# - Starting API in background
# - Installing UI dependencies (npm install)
# - Starting Vite dev server with HMR
# - Waiting for API health check before launching UI
```

### Library-Only Development
```powershell
# Build .NET solution
dotnet build

# Run all .NET tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### API Development
```powershell
# Run API standalone
cd src/Retention.Api
dotnet run --launch-profile http     # http://localhost:5219
dotnet run --launch-profile https    # https://localhost:7080

# Swagger UI available at /swagger
# Health check at /health
```

### UI Development
```powershell
# Run UI standalone (requires API running separately)
cd src/retention-ui
npm install          # First time only
npm run dev          # Start Vite dev server on http://localhost:5173
npm run build        # Production build
npm test             # Run Vitest unit tests
npm run test:e2e     # Run Playwright E2E tests
npm run typecheck    # TypeScript type checking
npm run lint         # ESLint
npm run format       # Prettier format
```

### Test Organization
- **Unit Tests**: `tests/Retention.UnitTests/`
  - `Domain/RetentionPolicyEvaluatorTests.cs` - ranking, eligibility, tie-breakers
  - `Application/EvaluateRetentionServiceTests.cs` - validation, error codes, DTOs
- **Integration Tests**: `tests/Retention.IntegrationTests/`
  - `EndToEndTests.cs` - full scenarios with sample data
  - `EvaluateRetentionContractTests.cs` - determinism gate, output ordering
  - `TelemetryTests.cs` - observability instrumentation
- **API Integration Tests**: `tests/Retention.Api.IntegrationTests/`
  - HTTP contract tests, ProblemDetails validation, CORS, auth, rate limiting
- **UI Tests**: `src/retention-ui/tests/` and `src/retention-ui/e2e/`
  - Vitest for component/unit tests (with MSW for API mocking)
  - Playwright for E2E workflow tests

### Test Conventions (xUnit + FluentAssertions)
```csharp
// Pattern: Arrange-Act-Assert with descriptive names
[Fact]
public void TwoReleases_SameEnvironment_KeepOne_MostRecentlyDeployedIsKept()
{
    // Arrange
    var releases = CreateReleaseLookup(
        new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)),
        new Release("Release-2", "Project-1", "1.0.1", Date(2000, 1, 1, 9)));
    
    var deployments = new[]
    {
        new Deployment("Deployment-1", "Release-2", "Environment-1", Date(2000, 1, 1, 10)),
        new Deployment("Deployment-2", "Release-1", "Environment-1", Date(2000, 1, 1, 11))
    };

    // Act
    var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 1);

    // Assert
    result.Should().HaveCount(1);
    result[0].ReleaseId.Should().Be("Release-1", "it was deployed most recently at 11:00");
}
```

**Test helper pattern**: Use `CreateReleaseLookup()` and `Date()` helpers to reduce boilerplate.

### Determinism Gate (Required for PR)
Test must prove identical inputs produce identical outputs:
```csharp
// Run evaluation twice, assert byte-identical JSON serialization
var result1 = service.EvaluateRetention(...);
var result2 = service.EvaluateRetention(...);
JsonSerializer.Serialize(result1).Should().Be(JsonSerializer.Serialize(result2));
```

## Error Handling Strategy

Per [docs/03_Architecture.md](../docs/03_Architecture.md):

### Exception Types
- **`ValidationException`**: Input errors (e.g., `n < 0`), stable error codes like `validation.n_negative`
- **`DomainException`**: Domain invariant violations (rare - most rules return diagnostics instead)
- **Never catch** inside domain/application layers - only at host boundary (if present)

### Error Code Pattern
Use stable, dot-separated codes for programmatic handling:
```csharp
public static class ErrorCodes
{
    public const string NNegative = "validation.n_negative";
    // ...
}
```

## Key Files and Navigation

### Documentation Structure
- **Requirements**: [docs/02_Requirements.md](../docs/02_Requirements.md) - all REQ-xxxx and NFR-xxxx IDs
- **ADRs**: [docs/adr/](../docs/adr/) - architectural decisions (read before changing core design)
- **Test Spec**: [docs/14_Test_Specification.md](../docs/14_Test_Specification.md) - comprehensive test cases
- **Assumptions**: [docs/00_Assumptions_and_OpenQuestions.md](../docs/00_Assumptions_and_OpenQuestions.md) - record clarifications here

### Sample Input Data
[docs/retention-original-specification/](../docs/retention-original-specification/) contains JSON fixtures:
- `Projects.json`, `Environments.json`, `Releases.json`, `Deployments.json`
- Use for end-to-end test scenarios

### Entry Points
- **Library API**: `EvaluateRetentionService.EvaluateRetention()` in [EvaluateRetentionService.cs](../src/Retention.Application/EvaluateRetentionService.cs)
- **Domain Service**: `RetentionPolicyEvaluator.Evaluate()` in [RetentionPolicyEvaluator.cs](../src/Retention.Domain/Services/RetentionPolicyEvaluator.cs)
- **REST API**: `/api/v1/retention/evaluate` and `/api/v1/datasets/validate` in [Program.cs](../src/Retention.Api/Program.cs)
- **UI App**: [App.tsx](../src/retention-ui/src/App.tsx) - main React component with dataset upload/evaluation workflow

## Project Conventions

### C# Style
- **Nullability**: Enabled (`<Nullable>enable</Nullable>`) - use `required` for DTOs
- **Immutability**: Prefer `record` for entities/DTOs, `sealed` for services
- **Implicit usings**: Enabled - common namespaces auto-imported
- **Framework**: .NET 8 (`net8.0`)

### Decision Log Reason Codes
Defined in [DecisionLogEntry.cs](../src/Retention.Application/Models/DecisionLogEntry.cs):
- `kept.top_n` - Release is in top N most recently deployed
- `diagnostic.invalid_reference` - Deployment excluded due to missing entity reference

### Naming Patterns
- **Entities**: Noun records (e.g., `Release`, `Deployment`)
- **Services**: Verb-based suffix (e.g., `RetentionPolicyEvaluator`, `EvaluateRetentionService`)
- **DTOs**: Result/Entry/Diagnostics suffix (e.g., `RetentionResult`, `DecisionLogEntry`)
- **Tests**: `[Method]_[Scenario]_[ExpectedOutcome]` (e.g., `ReleaseWithNoDeployments_IsNotKept`)

### Performance Targets
Per [docs/03_Architecture.md](../docs/03_Architecture.md):
- Single-pass grouping of deployments
- O(D + R log R) per project/environment where D=deployments, R=releases
- Use dictionaries for lookups, avoid repeated LINQ materializations
- Sort only once per (project, environment) group for deterministic ranking

## Observability (Addendum)

Per [ADR-0007](../docs/adr/ADR-0007-observability-opentelemetry.md):
- **Instrumentation**: `System.Diagnostics.ActivitySource` and `Meter` (no OTel package dependency)
- **Telemetry class**: [RetentionTelemetry.cs](../src/Retention.Application/Observability/RetentionTelemetry.cs)
- **Metrics**: `retention.evaluations`, `retention.kept_releases`, `retention.invalid_deployments`, `retention.evaluation_duration`
- **Traces**: `retention.evaluate` activity with tags for input counts and outputs
- **Host responsibility**: Configure ActivityListener/MeterListener; library only emits data

## What NOT to Build (Out of Scope)
- Database persistence (library operates on in-memory datasets)
- Physical deletion execution (discussed in addendum only)
- Authentication/Authorization implementation (API has hooks but uses demo/stub auth)

## When Making Changes

1. **Requirements first**: Check [docs/02_Requirements.md](../docs/02_Requirements.md) and [ADRs](../docs/adr/) before implementing
2. **Tests alongside code**: Update or add tests in same commit (TDD-friendly)
3. **Maintain determinism**: No `DateTime.Now`, no randomness, no unstable ordering
4. **Keep domain pure**: No I/O, logging, or infrastructure in Domain layer
5. **Document assumptions**: Add to [docs/00_Assumptions_and_OpenQuestions.md](../docs/00_Assumptions_and_OpenQuestions.md) if clarifying ambiguity
6. **Update README**: Per REQ-0011, document AI-assistance usage if applicable
