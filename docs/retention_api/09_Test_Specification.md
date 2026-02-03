# Comprehensive Test Specification (API)

This specification defines **unit**, **component**, **integration**, and **end-to-end** test coverage for the Release Retention Service API and its in-process evaluator (“backend” within this solution).

Scope drivers:
- Deterministic output ordering and diagnostics/decision log ordering. [Source: docs/inputs/requirements_source.md#API-REQ-0001-—-Evaluate-Retention-stateless] [Source: docs/inputs/requirements_source.md#API-REQ-0008-—-Deterministic-DiagnosticsDecision-Log]
- Boundary validation and consistent ProblemDetails errors. [Source: docs/inputs/requirements_source.md#API-REQ-0006-—-Input-Validation-at-the-Boundary] [Source: docs/inputs/requirements_source.md#API-REQ-0007-—-Error-Handling-via-ProblemDetails]
- OTel-friendly observability. [Source: docs/inputs/requirements_source.md#API-NFR-0003-—-Observability-OTel-Friendly]

---

## 1. Test Levels and Responsibilities

### 1.1 Unit Tests (fast, pure)
**Goal:** determinism + correctness of pure functions (normalization, sorting, validation rules, ProblemDetails mapping helpers).

- Project: `tests/Retention.UnitTests`
- No ASP.NET host; no HTTP.

### 1.2 Application/Component Tests (in-process, minimal wiring)
**Goal:** verify use-cases (Validate / Evaluate) with in-process evaluator via `IRetentionEvaluator` interface, including deterministic ordering and reason codes.

- Project: `tests/Retention.UnitTests` (or `tests/Retention.Application.Tests` if split)
- In-memory objects only.

### 1.3 API Integration Tests (HTTP boundary)
**Goal:** verify controllers, model binding, middleware, ProblemDetails envelope, health endpoints, OpenAPI surface, and feature toggles (auth/rate limiting).

- Project: `tests/Retention.Api.IntegrationTests`
- Uses `WebApplicationFactory<Program>` (or equivalent).
- Asserts JSON shape and ordering determinism.

### 1.4 Contract Tests (OpenAPI)
**Goal:** ensure OpenAPI describes the actual runtime behaviour and includes required schemas.

- Implement as part of API integration suite.
- Focus: endpoint paths, response codes, ProblemDetails schema, examples. [Source: docs/inputs/requirements_source.md#API-REQ-0004-—-OpenAPI-Definition]

### 1.5 Observability Tests (ActivityListener)
**Goal:** verify trace spans/attributes without an exporter. [Source: docs/inputs/requirements_source.md#8.-Testing-Requirements-API]

- Implement in unit or integration suite.
- Uses `ActivityListener` to capture spans around evaluate/validate calls.

---

## 2. Deterministic Test Fixtures (Canonical Datasets)

All tests that depend on datasets MUST use canonical fixtures with deterministic serialization.

### 2.1 Fixture Set (minimal, deterministic)
- `Fixture-A`: 1 project / 1 environment / N releases, simple deployments.
- `Fixture-B`: 2 projects / 2 environments / mixed deployments; includes ties to exercise tie-breakers.
- `Fixture-C`: includes invalid references and null elements to drive diagnostics and validation errors. [Source: docs/inputs/requirements_source.md#API-REQ-0002-—-Validate-Dataset]

### 2.2 Shuffled Variants
For each fixture, produce shuffled variants:
- Shuffle array order at every level (projects, environments, releases, deployments).
- Ensure duplicates are not introduced.

**Acceptance criterion enforced by tests:** shuffled inputs produce byte-for-byte identical output JSON (using the same serializer configuration as the API). [Source: docs/inputs/requirements_source.md#API-REQ-0001-—-Evaluate-Retention-stateless]

---

## 3. Test Cases (API + Backend Evaluator)

### 3.1 Boundary Validation and ProblemDetails

#### API-TST-0001 — Negative releasesToKeep returns 400 ProblemDetails
- **Requirement mapping:** REQ-0006, REQ-0007. [Source: docs/inputs/requirements_source.md#API-REQ-0006-—-Input-Validation-at-the-Boundary] [Source: docs/inputs/requirements_source.md#API-REQ-0007-—-Error-Handling-via-ProblemDetails]
- **Setup:** `POST /api/v1/retention/evaluate` with `releasesToKeep = -1` and valid dataset.
- **Assert:**
  - HTTP 400
  - `Content-Type: application/problem+json`
  - body contains:
    - `status = 400`
    - `error_code = "validation.n_negative"`
    - `trace_id` present (non-empty)
    - `correlation_id` present if request includes correlationId
  - response contains no stack trace / exception details.

#### API-TST-0002 — Invalid JSON returns 400 ProblemDetails (model binding)
- **Requirement mapping:** REQ-0006, REQ-0007.
- **Setup:** send malformed JSON.
- **Assert:** 400 ProblemDetails; stable `error_code` assigned for JSON parse failure (define deterministically, e.g., `validation.json_invalid`).

> NOTE: only `validation.n_negative` is explicitly mandated; other error codes must be defined deterministically in code and documented in OpenAPI examples. [Source: docs/inputs/requirements_source.md#API-REQ-0007-—-Error-Handling-via-ProblemDetails]

#### API-TST-0003 — Payload size limits enforced
- **Requirement mapping:** REQ-0006. [Source: docs/inputs/requirements_source.md#API-REQ-0006-—-Input-Validation-at-the-Boundary]
- **Setup:** configure small max request size for test environment; submit payload exceeding the limit.
- **Assert:** 413 or 400 ProblemDetails (choose one approach, document it); stable `error_code` (e.g., `validation.payload_too_large`).

### 3.2 Validate Endpoint

#### API-TST-0101 — Validate returns isValid=false with deterministic errors ordering
- **Requirement mapping:** REQ-0002. [Source: docs/inputs/requirements_source.md#API-REQ-0002-—-Validate-Dataset]
- **Setup:** submit Fixture-C.
- **Assert:**
  - 200 OK
  - `isValid=false`
  - `errors[]` sorted by stable key `(code, path, message)` using ordinal comparison
  - `warnings[]` similarly sorted
  - `summary` counts correct

#### API-TST-0102 — Validate detects null elements and missing required fields
- **Requirement mapping:** REQ-0002.
- **Setup:** dataset with `[null]` element within arrays and missing required fields.
- **Assert:** error entries with paths to offending elements; no non-deterministic ordering.

### 3.3 Evaluate Endpoint (API & Backend Integration)

#### API-TST-0201 — Evaluate returns required fields and deterministic ordering
- **Requirement mapping:** REQ-0001, REQ-0008. [Source: docs/inputs/requirements_source.md#API-REQ-0001-—-Evaluate-Retention-stateless] [Source: docs/inputs/requirements_source.md#API-REQ-0008-—-Deterministic-DiagnosticsDecision-Log]
- **Setup:** submit Fixture-B with `releasesToKeep=n`.
- **Assert:**
  - 200 OK
  - response includes `keptReleases`, `decisions`, `diagnostics`
  - `keptReleases` stable sort key is explicitly enforced (documented in DSDS `docs/03_Architecture.md`)
  - `decisions` stable ordering is enforced and includes stable reason codes (e.g., `kept.top_n`, `diagnostic.invalid_reference`). [Source: docs/inputs/requirements_source.md#API-REQ-0008-—-Deterministic-DiagnosticsDecision-Log]

#### API-TST-0202 — Determinism under shuffled inputs (golden output)
- **Requirement mapping:** REQ-0001. [Source: docs/inputs/requirements_source.md#API-REQ-0001-—-Evaluate-Retention-stateless]
- **Setup:** compare outputs for Fixture-B vs shuffled variant.
- **Assert:** outputs identical, including ordering of arrays.

#### API-TST-0203 — Per-(Project,Environment) grouping respects top-N selection deterministically
- **Requirement mapping:** REQ-0001 and “top N per (Project,Environment)” assumption. [Source: docs/inputs/requirements_source.md#3.-Assumptions-and-Open-Questions]
- **Setup:** dataset with multiple projects/environments and `releasesToKeep=1`.
- **Assert:** exactly 1 kept per (project,environment) group, chosen deterministically per tie-breakers.

### 3.4 Health Endpoints

#### API-TST-0301 — /health/live returns 200 when process running
- **Requirement mapping:** REQ-0003. [Source: docs/inputs/requirements_source.md#API-REQ-0003-—-Health-Endpoints]

#### API-TST-0302 — /health/ready returns 200 when ready (no external deps)
- **Requirement mapping:** REQ-0003.

### 3.5 OpenAPI / Swagger

#### API-TST-0401 — OpenAPI includes v1 endpoints and ProblemDetails schema
- **Requirement mapping:** REQ-0004, REQ-0005, REQ-0007. [Source: docs/inputs/requirements_source.md#API-REQ-0004-—-OpenAPI-Definition] [Source: docs/inputs/requirements_source.md#API-REQ-0005-—-API-Versioning] [Source: docs/inputs/requirements_source.md#API-REQ-0007-—-Error-Handling-via-ProblemDetails]
- **Setup:** fetch swagger json.
- **Assert:** paths exist, responses documented, schema includes extensions (`error_code`, `trace_id`, `correlation_id`).

### 3.6 Auth Toggle (when enabled)

#### API-TST-0501 — Missing token returns 401 when auth enabled
- **Requirement mapping:** REQ-0009. [Source: docs/inputs/requirements_source.md#API-REQ-0009-—-AuthenticationAuthorization-Configurable]
- **Setup:** enable auth in test host config; call evaluate without token.
- **Assert:** 401; confirm endpoints can be restricted by policy (if implemented).

### 3.7 Rate Limiting (when enabled)

#### API-TST-0601 — Exceed rate limit returns 429 ProblemDetails
- **Requirement mapping:** REQ-0010. [Source: docs/inputs/requirements_source.md#API-REQ-0010-—-Rate-Limiting]
- **Setup:** configure small limit; issue requests in loop.
- **Assert:** 429 + PD with `error_code="rate_limited"`.

### 3.8 Observability

#### API-TST-0701 — Validate emits span with required attributes
- **Requirement mapping:** NFR-0003. [Source: docs/inputs/requirements_source.md#API-NFR-0003-—-Observability-OTel-Friendly]
- **Setup:** capture spans via ActivityListener while calling validate.
- **Assert:** span exists with attributes: counts (`projects_count`, `deployments_count`, etc.).

#### API-TST-0702 — Evaluate emits span with releases_to_keep and invalid_deployments_excluded
- **Requirement mapping:** NFR-0003.
- **Assert:** span attributes present and deterministic values.

---

## 4. Traceability Matrix (API)

| Requirement | Test cases |
|---|---|
| REQ-0001 | API-TST-0201, 0202, 0203 |
| REQ-0002 | API-TST-0101, 0102 |
| REQ-0003 | API-TST-0301, 0302 |
| REQ-0004 | API-TST-0401 |
| REQ-0005 | API-TST-0401 |
| REQ-0006 | API-TST-0001, 0002, 0003 |
| REQ-0007 | API-TST-0001, 0002, 0401, 0601 |
| REQ-0008 | API-TST-0201 |
| REQ-0009 | API-TST-0501 |
| REQ-0010 | API-TST-0601 |
| NFR-0003 | API-TST-0701, 0702 |

---

## 5. Cross-System Integration (UI ↔ API) Notes (API-side obligations)

To support UI integration/E2E:
- Ensure CORS can be enabled via configuration for dev/test.
- Keep error contract stable and documented in OpenAPI examples. [Source: docs/inputs/requirements_source.md#API-REQ-0004-—-OpenAPI-Definition]
- Maintain stable ordering guarantees in API responses for UI tables. [Source: docs/inputs/requirements_source.md#API-REQ-0001-—-Evaluate-Retention-stateless]
