# API Requirements Specification — Release Retention Service (.NET)

## 1. Purpose

Expose the retention evaluator as a **RESTful JSON API** for clients (UI/automation) to validate input datasets and compute deterministic retention outcomes.

## 2. Scope

**In-scope**

* Stateless evaluation endpoint returning kept releases + decision log + diagnostics.
* Dataset validation endpoint returning structured errors/warnings.
* Health endpoints (liveness/readiness).
* OpenAPI (Swagger) contract.
* Consistent error handling (RFC7807 ProblemDetails).
* Observability hooks (OpenTelemetry-friendly).

**Out of scope (unless explicitly added later)**

* Persistent storage of datasets/evaluations.
* Coordinated deletion of releases/deployments/artifacts/logs.

## 3. Assumptions and Open Questions

**Assumptions**

* Domain objects: Project, Environment, Release, Deployment.
* Evaluation selects **top N releases per (Project, Environment)** using deterministic ranking rules already implemented.

**Open questions**

* **Persistence?** If storing evaluations/datasets, add list endpoints with pagination/filtering and caching.
* **Auth required?** If beyond local/dev, require JWT/OAuth and role-based authorization.
* **Multi-tenant?** If yes, all operations become tenant-scoped and audited.

## 4. Design Constraints

* ASP.NET Core Web API (modern supported LTS).
* RESTful routing with attribute routing.
* JSON for request/response.
* OpenAPI contract published and kept current.

---

## 5. Functional Requirements

### API-REQ-0001 — Evaluate Retention (stateless)

**Route:** `POST /api/v1/retention/evaluate`
**Description:** Evaluate a dataset with `releasesToKeep = n`.
**Acceptance criteria**

1. Same input → same output (including ordering).
2. Response includes: `keptReleases`, `decisions`, `diagnostics`.
3. Validation failures return `400` ProblemDetails with stable `error_code`.

### API-REQ-0002 — Validate Dataset

**Route:** `POST /api/v1/datasets/validate`
**Description:** Validate dataset structure and referential integrity without evaluating.
**Acceptance criteria**

1. Returns `isValid`, `errors[]`, `warnings[]`, `summary`.
2. Errors/warnings are deterministic and ordered (stable sort key).
3. Reports null elements and missing required fields.

### API-REQ-0003 — Health Endpoints

**Routes:** `GET /health/live`, `GET /health/ready`
**Acceptance criteria**

1. `live` returns 200 when process is running.
2. `ready` returns 200 when service is ready to accept requests (no external deps assumed unless later introduced).

### API-REQ-0004 — OpenAPI Definition

**Description:** Publish OpenAPI describing endpoints, DTOs, response codes, and ProblemDetails model.
**Acceptance criteria**

1. Contract includes schemas and examples for all requests/responses.
2. Swagger UI available in non-prod or dev.

### API-REQ-0005 — API Versioning

**Description:** Version via path segment `/api/v1/...`.
**Acceptance criteria**

1. v1 routes remain stable.
2. v2 can coexist without breaking v1.

### API-REQ-0006 — Input Validation at the Boundary

**Description:** Validate all incoming data before invoking evaluator.
**Acceptance criteria**

1. `releasesToKeep < 0` → `400` with `error_code = validation.n_negative`.
2. Missing/invalid fields produce actionable validation messages.
3. Payload size limits enforced (configurable).

### API-REQ-0007 — Error Handling via ProblemDetails

**Description:** Return RFC7807 ProblemDetails consistently.
**Acceptance criteria**

1. No stack traces or sensitive internals in responses.
2. Include stable `error_code` and `trace_id` (and `correlation_id` if provided).

### API-REQ-0008 — Deterministic Diagnostics/Decision Log

**Description:** Return decision entries (kept + diagnostics) and summary counts.
**Acceptance criteria**

1. Decision ordering is stable and documented.
2. Reason codes are stable strings (e.g., `kept.top_n`, `diagnostic.invalid_reference`).

### API-REQ-0009 — Authentication/Authorization (Configurable)

**Description:** Support JWT auth for non-local deployments.
**Acceptance criteria**

1. When enabled: missing/invalid token → 401.
2. Authorization policy can restrict compute endpoints if needed.

### API-REQ-0010 — Rate Limiting

**Description:** Apply basic rate limiting to compute endpoints.
**Acceptance criteria**

1. Limit exceeded → 429 ProblemDetails with `error_code = rate_limited`.

---

## 6. Non-Functional Requirements

### API-NFR-0001 — Performance

* Efficient for typical interview-scale datasets.
* Avoid unnecessary allocations in hot paths; no synchronous blocking I/O in request handlers.

### API-NFR-0002 — Reliability

* No retries for validation/domain failures.
* If external dependencies are added later, retries must be bounded and for transient errors only.

### API-NFR-0003 — Observability (OTel-Friendly)

* Traces: span around evaluate/validate operations with attributes:

  * counts (`projects_count`, `deployments_count`, etc.)
  * `releases_to_keep`
  * `invalid_deployments_excluded`
* Metrics: request count/duration; failure count by `error_code`.
* Logs: structured at boundaries; single-log-per-failure with correlation/trace id.

### API-NFR-0004 — Security

* Validate inputs; enforce request size limits; avoid logging payloads by default.
* HTTPS assumed at ingress in real deployment.

### API-NFR-0005 — Maintainability

* Domain logic remains isolated (clean architecture).
* API layer uses DTOs; domain entities are not exposed.

---

## 7. API Contracts

### 7.1 Evaluate Request

* `dataset`: projects/environments/releases/deployments
* `releasesToKeep`: integer >= 0
* `correlationId`: optional string

### 7.2 Evaluate Response

* `keptReleases[]`: stable-ordered kept items
* `decisions[]`: stable-ordered kept + diagnostics
* `diagnostics`: counts and summary

### 7.3 Validation Response

* `isValid`: boolean
* `errors[]`, `warnings[]`: messages `{ code, message, path? }`
* `summary`: counts

### 7.4 Error Response

* `application/problem+json` with `error_code`, `trace_id`, `correlation_id` extensions.

---

## 8. Testing Requirements (API)

* Contract tests for OpenAPI shapes and status codes.
* Integration tests:

  * determinism under shuffled inputs
  * validation errors and ProblemDetails shape
  * health endpoints
  * optional ActivityListener validation for traces (no exporter required)
