# Pattern 05 — Strategy (Ranking + Selection Policy)

## Intent
Enable retention policy variations without editing the evaluator core. Default behavior MUST match current implementation exactly.

## Design
In `Retention.Domain.Services` introduce:
- `IRetentionRankingStrategy`
  - `IReadOnlyList<RankedCandidate> Rank(IReadOnlyList<GroupEntry> entries, IReleaseMetadataProvider metadataProvider)`
- `IRetentionSelectionStrategy`
  - `IReadOnlyList<RankedCandidate> Select(IReadOnlyList<RankedCandidate> ranked, int releasesToKeep)`

Default strategies:
- `DefaultRankingStrategy`: implements ADR-0003 tie-breakers:
  1) LatestDeployedAt desc
  2) Release.Created desc
  3) Release.Id asc (ordinal)
- `TopNSelectionStrategy`: takes first `n`

## Requirements
- STRAT-REQ-0001: For all inputs, default strategy outputs MUST match current `RetentionPolicyEvaluator` outputs (including deterministic ordering).
- STRAT-REQ-0002: Strategy interfaces MUST be internal to domain; application layer selects default via evaluator constructor unless configured otherwise.

## Acceptance criteria
- Golden tests comparing old and new evaluator outputs pass for multiple datasets including tie cases.

## Notes
Do not expose strategy variability via public API unless required; keep injection available for future extension.
