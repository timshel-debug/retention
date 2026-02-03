# Requirements (API)

This DSDS reflects only the **API** requirements in `docs/inputs/requirements_source.md`.

## Functional requirements

| ID | Name | Summary | Source |
|---|---|---|---|
| REQ-0001 | Evaluate retention | `POST /api/v1/retention/evaluate` returns kept releases + decisions + diagnostics with deterministic ordering. | [Source: docs/inputs/requirements_source.md#API-REQ-0001-—-Evaluate-Retention-stateless] |
| REQ-0002 | Validate dataset | `POST /api/v1/datasets/validate` returns deterministic errors/warnings and summary counts. | [Source: docs/inputs/requirements_source.md#API-REQ-0002-—-Validate-Dataset] |
| REQ-0003 | Health endpoints | `GET /health/live` and `GET /health/ready`. | [Source: docs/inputs/requirements_source.md#API-REQ-0003-—-Health-Endpoints] |
| REQ-0004 | OpenAPI definition | Publish OpenAPI with schemas and examples; Swagger UI in dev/non-prod. | [Source: docs/inputs/requirements_source.md#API-REQ-0004-—-OpenAPI-Definition] |
| REQ-0005 | API versioning | Version via path segment `/api/v1/...` and allow v2 coexistence. | [Source: docs/inputs/requirements_source.md#API-REQ-0005-—-API-Versioning] |
| REQ-0006 | Boundary validation | Validate all incoming data and enforce payload size limits. | [Source: docs/inputs/requirements_source.md#API-REQ-0006-—-Input-Validation-at-the-Boundary] |
| REQ-0007 | ProblemDetails errors | Return RFC7807 ProblemDetails with stable `error_code`, `trace_id`, `correlation_id` if provided; no stack traces. | [Source: docs/inputs/requirements_source.md#API-REQ-0007-—-Error-Handling-via-ProblemDetails] |
| REQ-0008 | Deterministic decision log | Stable-ordered decisions with stable reason codes. | [Source: docs/inputs/requirements_source.md#API-REQ-0008-—-Deterministic-DiagnosticsDecision-Log] |
| REQ-0009 | Auth (configurable) | Support JWT auth when enabled. | [Source: docs/inputs/requirements_source.md#API-REQ-0009-—-AuthenticationAuthorization-Configurable] |
| REQ-0010 | Rate limiting | Rate limit compute endpoints; exceed -> 429 ProblemDetails with `error_code=rate_limited`. | [Source: docs/inputs/requirements_source.md#API-REQ-0010-—-Rate-Limiting] |

## Non-functional requirements

| ID | Name | Summary | Source |
|---|---|---|---|
| NFR-0001 | Performance | Efficient for typical datasets; avoid synchronous blocking I/O in handlers. | [Source: docs/inputs/requirements_source.md#API-NFR-0001-—-Performance] |
| NFR-0002 | Reliability | No retries for validation/domain failures; bounded retries only for future transient deps. | [Source: docs/inputs/requirements_source.md#API-NFR-0002-—-Reliability] |
| NFR-0003 | Observability | OTel-friendly traces/metrics/logging with specified attributes and failure counts by error_code. | [Source: docs/inputs/requirements_source.md#API-NFR-0003-—-Observability-OTel-Friendly] |
| NFR-0004 | Security | Validate inputs, enforce size limits, avoid payload logging. | [Source: docs/inputs/requirements_source.md#API-NFR-0004-—-Security] |
| NFR-0005 | Maintainability | Keep domain logic isolated; API layer uses DTOs. | [Source: docs/inputs/requirements_source.md#API-NFR-0005-—-Maintainability] |
