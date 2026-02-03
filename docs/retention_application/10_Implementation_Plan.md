# Implementation Plan

## SLICE-0001 Core Domain Policy (Selection)
- Deliverables:
  - `RetentionPolicyEvaluator` with eligibility + ranking + tie-breakers
  - Multi-environment evaluation grouping by `(ProjectId, EnvironmentId)`
  - Unit tests for REQ-0002..REQ-0006
- Maps to: REQ-0002, REQ-0003, REQ-0004, REQ-0005, REQ-0006; NFR-0003
- Stop Point:
  - Deterministic selection proven via unit tests (including multi-environment sample)

## SLICE-0002 Application Use Case + DTOs
- Deliverables:
  - `EvaluateRetentionUseCase`
  - `RetentionResult`, `DecisionLogEntry`, deterministic ordering
  - Validation + typed errors
- Maps to: REQ-0001, REQ-0007, REQ-0008, REQ-0009, REQ-0010; NFR-0001, NFR-0002
- Stop Point:
  - Contract tests validate stable output and error codes

## SLICE-0003 Repository Deliverables
- Deliverables:
  - `README.md` including AI-assistance disclosure section (if AI used)
  - Document assumptions and ideas for improvements
- Maps to: REQ-0011
- Stop Point:
  - README checklist passes

## Optional Enhancements (Not required by coding exercise)
- Coordinated deletion planning and adapter execution (ADR-0006) can be added after core requirements are complete.

## Dependencies
- None required beyond .NET runtime and test framework (exact .NET LTS version TODO).

## Optional Addendum Slices

SLICE-0006 — Observability instrumentation (OTel)
- Deliver: add tracing/metrics/log hooks per `docs/12_Observability_Addendum.md`.
- Maps: NFR-0006 (decision logging), NFR-0003 (determinism), NFR-0008 (observability addendum).

SLICE-0007 — Coordinated deletion boundary (future)
- Deliver: `DeletionPlan` derivation + `IReleaseDeletionCoordinator` boundary in host layer per `docs/13_Coordinated_Deletion_Addendum.md`.
- Maps: ADR-0006.
