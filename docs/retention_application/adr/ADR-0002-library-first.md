# ADR-0002 Library-First API with Optional Worker Host

## Context
Core logic must not require UI/CLI/DB; evaluation should be embeddable and testable. [Source: Start Here - Instructions - Release Retention.md#The Task]

## Decision
Expose `EvaluateRetention` as a library contract. Provide an optional worker host for boundary-only catch/logging and future deletion orchestration.

## Options Considered
1. HTTP API (rejected: not required; would conflict with constraints)
2. Library-first with optional host (chosen)

## Consequences
- Satisfies constraints while enabling operational boundary
- If an API is later needed, wrap without changing domain logic
