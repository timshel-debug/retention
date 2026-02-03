# ADR-0006 Coordinated Deletion (Idea Only, Out of Scope)

## Context
The exercise motivation mentions storage pressure from logs/artifacts for deployments, but the task requirements do not require implementing deletion or defining store contracts. [Source: Start Here - Instructions - Release Retention.md:L10-L22]

## Decision
Treat coordinated deletion as a discussion topic / future enhancement:
- Do not require deletion planning or adapters to satisfy the coding exercise.
- Keep core retention logic isolated and testable.

## Options Considered
1. Implement deletion planning + adapter now (rejected: out of scope for exercise)
2. Defer as enhancement (chosen)

## Consequences
- Implementation remains focused on the explicit exercise requirements.
- If deletion is later required, introduce a `DeletionPlan` and adapter boundary via a new ADR and requirements.
