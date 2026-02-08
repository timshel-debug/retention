# Pattern 01 — Pipeline Orchestration (Step-based Evaluation)

## Intent
Replace the monolithic `EvaluateRetentionCore` method with an explicit, ordered pipeline of steps operating on a shared evaluation context.

## Design
Introduce:

- `Retention.Application.Evaluation.RetentionEvaluationContext`
- `Retention.Application.Evaluation.IEvaluationStep`
- Concrete steps, executed in order:

1. `NormalizeInputsStep`
2. `ValidateInputsStep`
3. `BuildReferenceIndexStep`
4. `FilterInvalidDeploymentsStep`
5. `EvaluatePolicyStep`
6. `MapResultsStep`
7. `BuildDecisionLogStep`
8. `ComputeDiagnosticsStep`
9. `FinalizeResultStep` (optional; can be part of previous steps)

### Context object (minimum)
`RetentionEvaluationContext` MUST include:
- Inputs: `Projects`, `Environments`, `Releases`, `Deployments`, `ReleasesToKeep`, `CorrelationId`
- Derived: `ReferenceIndex`, `ValidDeployments`, `DiagnosticDecisionEntries`
- Domain: `DomainCandidates`
- Outputs-in-progress: `KeptReleases`, `KeptDecisionEntries`, `AllDecisionEntries`, `Diagnostics`, `Result`

Context SHOULD be mutable for performance, but step boundaries MUST be explicit.

## Requirements

### Functional
- PIPE-REQ-0001: Pipeline MUST preserve current evaluation semantics (see bundle overview invariants).
- PIPE-REQ-0002: Pipeline MUST be deterministic: given identical inputs, the step execution order and outputs are identical.
- PIPE-REQ-0003: Each step MUST be unit-testable without requiring other steps (via pre-built context).

### Non-functional
- PIPE-NFR-0001: Pipeline refactor MUST NOT degrade performance by more than 10% on a representative dataset (define benchmark test).
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
