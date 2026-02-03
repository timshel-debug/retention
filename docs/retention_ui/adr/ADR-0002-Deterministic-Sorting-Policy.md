# ADR-0002: Deterministic sorting policy (UI)

## Context
UI must be deterministic and not rely on object-key iteration order. [Source: docs/inputs/requirements_source.md#UI-NFR-0004-—-Determinism]

## Decision
Implement explicit stable sort utilities and apply them to validation messages and result tables with documented tie-breakers.

## Consequences
Deterministic rendering regardless of API or input ordering.
