# ADR-0005 Invalid Reference Handling (Exclude + Diagnose)

## Context
Sample deployments include environment references not present in environments list; behavior is unspecified. [Source: Deployments.json]

## Decision
Exclude invalid deployments from eligibility calculations and add a diagnostic decision entry.

## Options Considered
1. Throw and fail evaluation (rejected: reduces robustness)
2. Implicitly create missing entities (rejected: invents data)
3. Exclude + diagnose (chosen)

## Consequences
- Evaluation continues and remains deterministic
- Requires upstream remediation for data integrity
