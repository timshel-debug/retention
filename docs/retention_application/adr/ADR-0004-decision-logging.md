# ADR-0004 Decision Logging via Returned DecisionLog + Optional Structured Logs

## Context
Requirements mandate “Log why a release should be kept” and testability. [Source: Start Here - Instructions - Release Retention.md#The Task]

## Decision
Return a `DecisionLog` in the result. Optionally emit structured logs at the host boundary using the same entries.

## Options Considered
1. Logging side-effects inside domain (rejected: harms testability)
2. Return decision entries (chosen)

## Consequences
- Tests can assert decision reasons
- Embedders choose logging sink and verbosity
