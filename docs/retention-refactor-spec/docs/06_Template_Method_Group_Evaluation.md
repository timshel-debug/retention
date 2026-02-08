# Pattern 06 — Template Method (Per-Group Evaluation Skeleton)

## Intent
Make the per-(ProjectId, EnvironmentId) evaluation flow explicit and overridable internally, supporting instrumentation and policy variation cleanly.

## Design
Introduce an internal abstraction in domain:
- `IGroupRetentionEvaluator` or `GroupRetentionEvaluatorBase`

Skeleton steps:
1. Compute eligible entries for the group
2. Invoke group-begin hook (for telemetry; no business effect)
3. Rank candidates (Strategy)
4. Select candidates (Strategy)
5. Assign ranks (1..n)
6. Return candidates for aggregation

The concrete evaluator MUST use the default ranking/selection strategies.

## Requirements
- TM-REQ-0001: Group evaluation MUST not depend on dictionary enumeration order; all ordering must be explicit.
- TM-REQ-0002: Hooks MUST be side-effect-free from the evaluator’s point of view (i.e., failures propagate, but no behavioral changes).

## Acceptance criteria
- Group evaluation produces identical ranks and ordering for tie cases.

## Why this matters
It allows accurate per-group telemetry spans (Pattern 08) around actual ranking/selection work.
