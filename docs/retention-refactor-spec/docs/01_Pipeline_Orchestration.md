# Pattern 01 — Pipeline Orchestration (Step-based Evaluation)

## Intent
Replace the monolithic `EvaluateRetentionCore` method with an explicit, ordered pipeline of steps operating on a shared evaluation context.

## Design
Introduce:

- `Retention.Application.Evaluation.RetentionEvaluationContext`
- `Retention.Application.Evaluation.IEvaluationStep`
- Concrete steps, executed in order:

1. `ValidateInputsStep` — validates inputs via the rule chain
2. `BuildReferenceIndexStep` — builds lookup dictionaries
3. `FilterInvalidDeploymentsStep` — produces `FilteredDeploymentsResult` (Pattern 04)
4. `EvaluatePolicyStep` — runs domain retention evaluator
5. `MapResultsStep` — maps domain candidates to kept releases
6. `BuildDecisionLogStep` — assembles combined decision log
7. `FinalizeResultStep` — computes diagnostics and assembles `RetentionResult`

**Implementation notes:**
- `NormalizeInputsStep` is handled in the imperative shell (`EvaluateRetentionService`) before engine entry, since null-coalescing is a shell concern (Pattern 10).
- `ComputeDiagnosticsStep` is merged into `FinalizeResultStep` for cohesion; the spec noted this was optional.

### Context object (minimum)
`RetentionEvaluationContext` MUST include:
- Inputs: `Projects`, `Environments`, `Releases`, `Deployments`, `ReleasesToKeep`, `CorrelationId`
- Derived: `ReferenceIndex`, `FilteredDeployments` (single result object — see Pattern 04)
- Domain: `DomainCandidates`
- Outputs-in-progress: `KeptReleases`, `KeptDecisionEntries`, `AllDecisionEntries`, `Diagnostics`, `Result`

Filtering outputs are carried as a single `FilteredDeploymentsResult` to prevent parallel-list drift. No separate `ValidDeployments`, `DiagnosticDecisionEntries`, or `InvalidExcludedCount` fields exist on the context.

## Requirements

### Functional
- PIPE-REQ-0001: Pipeline MUST preserve current evaluation semantics (see bundle overview invariants).
- PIPE-REQ-0002: Pipeline MUST be deterministic: given identical inputs, the step execution order and outputs are identical.
- PIPE-REQ-0003: Each step MUST be unit-testable without requiring other steps (via pre-built context).

### Non-functional
- PIPE-NFR-0001: Pipeline refactor MUST NOT degrade performance by more than 10% on a representative dataset. Benchmark: `dotnet run -c Release --project tests/Retention.Benchmarks/Retention.Benchmarks.csproj`
- PIPE-NFR-0002: No additional allocations in tight loops unless justified; prefer reusing lists in context.

## Acceptance criteria
- `EvaluateRetention(...)` produces identical `RetentionResult` for a set of golden test vectors (see Implementation Prompt).
- Exception types and error codes match current behavior.

## Suggested file layout
- `src/Retention.Application/Evaluation/RetentionEvaluationContext.cs`
- `src/Retention.Application/Evaluation/IEvaluationStep.cs`
- `src/Retention.Application/Evaluation/Steps/*.cs`

## Tests
- Pipeline “golden” tests: run engine on fixed fixtures and snapshot compare.
- Step tests: verify each step produces expected context mutations given minimal inputs.
