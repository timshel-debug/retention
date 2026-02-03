# API Contracts (consumed by UI)

- `POST /api/v1/datasets/validate` [Source: docs/inputs/requirements_source.md#UI-REQ-0004-—-Server-Side-Validation]
- `POST /api/v1/retention/evaluate` [Source: docs/inputs/requirements_source.md#UI-REQ-0006-—-Execute-Evaluation]
- Errors: RFC7807 ProblemDetails `application/problem+json` with `error_code`, `trace_id`, optional `correlation_id`. [Source: docs/inputs/requirements_source.md#UI-REQ-0010-—-Error-UX-ProblemDetails]
