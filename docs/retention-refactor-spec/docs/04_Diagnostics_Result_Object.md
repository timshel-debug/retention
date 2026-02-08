# Pattern 04 — Result Object / Notification (Diagnostics Accumulation)

## Intent
Stop accumulating parallel lists in the orchestration method; make filtering return a first-class result containing both valid deployments and diagnostic entries.

## Design
Introduce:
- `Retention.Application.Evaluation.FilteredDeploymentsResult`
  - `IReadOnlyList<Deployment> ValidDeployments`
  - `IReadOnlyList<DecisionLogEntry> DiagnosticEntries`
  - `int InvalidExcludedCount`

Filtering step returns this object.

## Requirements
- DIAG-REQ-0001: Filtering MUST not throw for invalid references; it MUST exclude invalid deployments and record a diagnostic entry.
- DIAG-REQ-0002: Diagnostic entry fields MUST match current behavior:
  - ProjectId is `release.ProjectId` if resolvable, else `"unknown"`
  - EnvironmentId, ReleaseId, N, Rank=0, LatestDeployedAt=null
  - ReasonCode=`DecisionReasonCodes.InvalidReference`
  - CorrelationId propagated

## Acceptance criteria
- For a fixture containing invalid references, `InvalidDeploymentsExcluded` matches current count and diagnostic entries match sorting behavior.

## Remember
This pattern is about shape and contract: do not change the diagnostic semantics.
