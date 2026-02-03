# ADR-0003 Deterministic Ranking and Tie-Breakers

## Context
Requirements define “most recently deployed” but do not define ties, and outputs must be deterministic. [Source: Start Here - Instructions - Release Retention.md#The Task]

## Decision
Rank releases per project/environment by:
1) latest deployment time desc (`max(DeployedAt)`),
2) release created desc,
3) release id asc.

## Options Considered
1. Arbitrary ordering (rejected: non-deterministic)
2. Additional semantic version parsing (rejected: not required by rule)
3. Deterministic tie-breakers (chosen)

## Consequences
- Deterministic outputs and stable tests
- If product semantics differ, update A-0007 and affected tests
