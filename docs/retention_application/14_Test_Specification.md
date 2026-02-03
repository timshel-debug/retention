# Comprehensive Test Specification

## Purpose
Define a complete, deterministic, implementation-ready test specification for the retention solution, covering unit, integration, and end-to-end tests. This document complements (does not replace) `docs/09_Test_Strategy_and_Gates.md`.

## Scope and Assumptions
- Primary target is the **library** retention engine (Domain + Application). Host/API concerns are optional and only specified where explicitly introduced by the DSDS addenda (e.g., OpenTelemetry instrumentation).
- No persistence layer or external services are required for the coding exercise; integration tests use in-memory fakes.
- All tests must be deterministic: no wall-clock time, randomness, or non-stable ordering.

## Traceability
Each test case references at least one requirement ID (REQ-xxxx / NFR-xxxx) defined in `docs/02_Requirements.md`. Where a requirement is not yet mapped, record a TODO in `docs/00_Assumptions_and_OpenQuestions.md`.

---

## Test Levels

### Unit Tests
**Project:** `Retention.*.UnitTests`  
**Framework:** xUnit (or NUnit) + FluentAssertions (or equivalent).  
**Target:** pure functions and deterministic policies; no file system; no network.

#### UT-001 Input Validation
- **Intent:** Validate all input DTO invariants and error taxonomy.
- **Covers:** REQ-0001, REQ-0007, NFR-0006 (if defined)
- **Cases:**
  1. Null/empty dataset handling (if API accepts it): returns validation error.
  2. Missing required identifiers: ProjectId/EnvironmentId/ReleaseId/DeploymentId.
  3. Invalid N values:
     - n < 0 => validation error
     - n = 0 => keep none (explicit)
     - n > distinct releases available => keep all distinct releases
  4. Duplicate deployments:
     - same DeploymentId repeated => either rejected or de-duped deterministically (must match DSDS rule).
  5. Unknown/unsupported retention unit (if input includes a policy type): validation error.

**Assertions:**
- Stable error code(s) and messages.
- No exceptions thrown for expected validation failures (use typed result).

#### UT-002 Eligibility Rules
- **Intent:** Ensure eligibility filter rules behave exactly per DSDS.
- **Covers:** REQ-0002..REQ-0004 (as applicable)
- **Cases:**
  1. Only successful deployments are eligible (if required).
  2. Exclude deployments older than cutoff (if required); if no time-based rules, assert none exist.
  3. Environment scoping: ensure decisions are per (ProjectId, EnvironmentId) group.

**Assertions:**
- Deterministic inclusion/exclusion; stable ordering.

#### UT-003 Ranking and Tie-Breakers
- **Intent:** Ensure deterministic selection ordering, including tie-breakers.
- **Covers:** REQ-0005, REQ-0006, NFR-0001
- **Cases (minimum set):**
  1. Single deployment -> selected.
  2. Multiple releases; keep top N; verify exact set.
  3. Tie on primary score: apply secondary tie-breaker (e.g., most recent deployment, then ReleaseVersion, then ReleaseId).
  4. Full tie scenario: identical attributes except IDs; ordering stable by ID ascending.
  5. Stability under input permutation: shuffle input deployments; output must be identical (set + order).

**Assertions:**
- Output ordering stable (documented comparator).
- No dependence on input ordering.

#### UT-004 Grouping Semantics
- **Intent:** Ensure grouping by (ProjectId, EnvironmentId) and then release grouping is correct.
- **Covers:** REQ-0003, REQ-0004
- **Cases:**
  1. Same project across two environments: treated independently.
  2. Two projects in same environment: treated independently.
  3. Multiple deployments for the same release in same group: release treated as one candidate; confirm aggregation rule (e.g., choose latest successful deployment).

#### UT-005 Decision Log Payload
- **Intent:** Ensure decision logging payload fields are complete and deterministic.
- **Covers:** ADR-0004, NFR-0007 (if defined)
- **Cases:**
  1. Logs include correlation/trace identifiers (when provided by host).
  2. Each candidate includes computed scores and reason codes.
  3. Log output is stable across runs for same input (excluding trace id if injected).

**Assertions:**
- Redaction: no secrets/PII.
- No stack traces.

#### UT-006 OpenTelemetry Instrumentation (Library-level)
- **Intent:** Validate that instrumentation is present but non-invasive.
- **Covers:** ADR-0007, NFR-0008
- **Cases:**
  1. When ActivitySource is enabled, `retention.evaluate` span is emitted.
  2. Span includes required attributes:
     - project_id, environment_id, releases_considered, releases_selected, n
  3. When no listener/exporter exists, overhead is negligible and behavior identical.
  4. Meter emits expected counters/histograms (if implemented).

**Assertions:**
- No exceptions even if exporter fails (host responsibility).
- No behavioral change when tracing disabled.

---

### Integration Tests
**Project:** `Retention.*.IntegrationTests`  
**Target:** composition of Application + adapters/fakes; still in-process.

#### IT-001 Public API Contract Test
- **Intent:** validate the public `EvaluateRetention` surface (DTOs, ordering guarantees).
- **Covers:** REQ-0008, NFR-0001
- **Setup:** build input dataset with multiple groups and ties.
- **Assertions:**
  - Response shape stable (contract).
  - Ordering stable and documented.
  - All IDs preserved correctly.

#### IT-002 In-Memory “Host” Wiring
- **Intent:** validate that library composes correctly when hosted (DI optional).
- **Covers:** NFR-0002
- **Setup:** create minimal host service that wires evaluator + logging + optional OTel.
- **Assertions:**
  - Evaluator can be invoked repeatedly without state leakage.
  - Structured logs include correlation id when provided.

#### IT-003 OTel Exporter Smoke (In-Memory)
- **Intent:** validate that spans/metrics export works end-to-end in-process.
- **Covers:** ADR-0007, NFR-0008
- **Setup:** OpenTelemetry SDK with in-memory exporter (test-only).
- **Assertions:**
  - One span per evaluation.
  - Attribute values correct.
  - Metrics increment as expected.

#### IT-004 Coordinated Deletion Boundary (Future / Optional)
- **Intent:** verify adapter boundary behavior without implementing actual deletion.
- **Covers:** ADR-0006, docs/13_Coordinated_Deletion_Addendum.md
- **Setup:** use fake `IReleaseDeletionCoordinator` with idempotency key tracking.
- **Assertions:**
  - DeletionPlan contains correct ids (only for non-retained items).
  - Coordinator invoked idempotently (replay safe).
  - Transient failures trigger retry policy only in host boundary (if implemented).

---

### End-to-End Tests
**Project:** `Retention.*.E2ETests` (optional)  
**Target:** packaged executable/host (console) invoking evaluator with realistic sample input.

#### E2E-001 Golden File Scenario
- **Intent:** validate stable end-to-end output for a known dataset.
- **Covers:** REQ-0002..REQ-0006, NFR-0001
- **Setup:**
  - Input dataset file under `tests/TestData/` (JSON).
  - Expected output file under `tests/Golden/` (JSON).
- **Assertions:**
  - Output exactly matches golden file (byte-for-byte or canonical JSON).
  - No timestamps or nondeterministic fields.

#### E2E-002 Observability Verification
- **Intent:** validate that E2E host emits telemetry when enabled.
- **Covers:** ADR-0007, NFR-0008
- **Setup:** run host with telemetry enabled (test exporter).
- **Assertions:**
  - At least one trace span emitted with correct operation name.
  - Metrics emitted (if implemented).

#### E2E-003 Failure Mode: Invalid Input
- **Intent:** validate stable failure response (not stack traces).
- **Covers:** REQ-0007
- **Setup:** invalid dataset.
- **Assertions:**
  - Exit code non-zero (if host exists).
  - Errors are structured, stable, and safe.

---

## Quality Gates
- Unit tests: required for core requirements slices (SLICE-0001..SLICE-0005).
- Integration tests: required for public contract + instrumentation smoke.
- E2E tests: optional for the exercise; include only if a host is created.

## Determinism Requirements (Test Harness)
- Use fixed, explicit IDs (GUIDs as constants if needed).
- Use fixed “now” injected via clock abstraction if any time rules exist; otherwise forbid time usage.
- Sort outputs explicitly before snapshot comparisons where ordering is not contractually relevant.

## Coverage Targets (Non-binding)
- Core evaluator: 90%+ statement coverage, 100% of ranking branches.
- Contract tests: cover all DTO fields and ordering invariants.
