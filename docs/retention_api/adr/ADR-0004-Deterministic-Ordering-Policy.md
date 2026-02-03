# ADR-0004: Deterministic ordering policy (API)

## Context
Same input must yield same output including ordering; diagnostics/decision log order must be stable. [Source: docs/inputs/requirements_source.md#API-REQ-0001-—-Evaluate-Retention-stateless]

## Decision
Normalize inputs and apply explicit stable sort keys for all output lists.

## Consequences
Hardens determinism against client list shuffling.
