# Pattern 07 — Mapper / Assembler (DTO + Decision Log)

## Intent
Separate mapping and text construction from orchestration. Keep formatting deterministic.

## Design
Introduce in application:
- `IKeptReleaseMapper`
  - `KeptRelease Map(ReleaseCandidate candidate)`
- `IDecisionLogAssembler`
  - `DecisionLogEntry BuildKeptEntry(ReleaseCandidate candidate, int releasesToKeep, string? correlationId)`
  - `DecisionLogEntry BuildInvalidDeploymentEntry(Deployment deployment, string projectId, int releasesToKeep, IReadOnlyList<string> reasons, string? correlationId)`
- `IDiagnosticsCalculator`
  - `RetentionDiagnostics Calculate(IReadOnlyList<ReleaseCandidate> candidates, int invalidExcludedCount, IReadOnlyList<KeptRelease> keptReleases)`

## Requirements
- MAP-REQ-0001: Kept release mapping MUST be field-for-field equivalent to current mapping.
- MAP-REQ-0002: Decision text MUST be deterministic and stable. Prefer byte-for-byte identical formatting to current strings.
- MAP-REQ-0003: Decision sorting MUST remain identical to current rules.

## Acceptance criteria
- Snapshot tests validate exact `ReasonText` strings for representative cases.
