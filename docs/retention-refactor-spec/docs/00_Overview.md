# Retention Refactor Requirements Specification (Bundle)

## Purpose
Refactor the current retention evaluation implementation to apply the following patterns end-to-end while preserving externally observable behavior:

1. Pipeline orchestration (step-based evaluation flow)
2. Chain of Responsibility for validation + reference checks
3. Specification for deployment validity and reason generation
4. Result object / notification for diagnostics accumulation
5. Strategy for ranking/selection policy variability
6. Template Method for per-(Project,Environment) group evaluation flow in domain
7. Mapper/Assembler for DTO conversion and decision log construction
8. Decorator for telemetry (replace callback-based “fake spans”)
9. Builder for reference index (lookup dictionaries)
10. Functional core + imperative shell separation (pure engine + telemetry shell)

Each pattern has its own spec document in this bundle.

## Scope
- Target code: `Retention.Application.EvaluateRetentionService` and `Retention.Domain.Services.RetentionPolicyEvaluator` and their immediate collaborators.
- Preserve `IEvaluateRetentionService.EvaluateRetention(...)` public signature and semantics.
- Preserve deterministic ordering rules and error codes/messages.

## Non-goals
- Changing retention business rules (Top N by latest deployment + tie-breakers) unless explicitly stated in the Strategy spec (defaults MUST match current).
- Changing DTO shapes (`RetentionResult`, `KeptRelease`, `DecisionLogEntry`) unless required to preserve behavior.
- Introducing new persistence or external side effects.

## Primary invariants to preserve
- Validation exceptions: same error codes, and messages equivalent in meaning (prefer identical).
- Null input lists treated as empty.
- Invalid deployments excluded and logged as diagnostics with `DecisionReasonCodes.InvalidReference`.
- Domain ranking tie-breakers (ADR-0003): `LatestDeployedAt desc`, then `Release.Created desc`, then `Release.Id asc (ordinal)`.
- Final output ordering: deterministic by `ProjectId asc`, `EnvironmentId asc`, `Rank asc` for kept releases; decision list ordering kept-before-diagnostic then keys.

## Document index
- `01_Pipeline_Orchestration.md`
- `02_Validation_Chain.md`
- `03_Deployment_Specification.md`
- `04_Diagnostics_Result_Object.md`
- `05_Retention_Strategy.md`
- `06_Template_Method_Group_Evaluation.md`
- `07_Mappers_and_Assemblers.md`
- `08_Telemetry_Decorator.md`
- `09_ReferenceIndex_Builder.md`
- `10_Functional_Core_Imperative_Shell.md`
- `99_Implementation_Prompt.md`
- `00_Assumptions_and_OpenQuestions.md`

## Glossary
- **Engine**: Pure evaluation core returning `RetentionResult` (no telemetry).
- **Shell**: Public application service method that manages telemetry/stopwatch/exception recording.
- **Group**: A `(ProjectId, EnvironmentId)` partition in domain evaluation.
