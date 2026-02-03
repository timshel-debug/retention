# Architecture (API)

## Clean Architecture layout

- `Retention.Api` (controllers, middleware, OpenAPI, health, auth/rate limiting)
- `Retention.Application` (handlers/use cases, normalization, validation orchestration)
- `Retention.Domain` (evaluator port + domain models + stable reason codes)
- `Retention.Infrastructure` (optional configuration helpers)

## Exception handling (boundary-only)

- Global middleware converts exceptions to RFC7807 ProblemDetails. [Source: docs/inputs/requirements_source.md#API-REQ-0007-—-Error-Handling-via-ProblemDetails]
- Validation failures should prefer *result objects* over exceptions; exceptions reserved for unexpected/unrecoverable cases.
- No stack traces or sensitive details in responses. [Source: docs/inputs/requirements_source.md#API-REQ-0007-—-Error-Handling-via-ProblemDetails]
- Always include `error_code` (stable), `trace_id`, optional `correlation_id`.

## Determinism enforcement

- Normalize input arrays by explicit stable sort keys before validate/evaluate.
- Explicit stable sort keys for:
  - errors/warnings `(code, path, message)` [Source: docs/inputs/requirements_source.md#API-REQ-0002-—-Validate-Dataset]
  - kept releases `(projectId, environmentId, rank, releaseId)` [Source: docs/inputs/requirements_source.md#API-REQ-0001-—-Evaluate-Retention-stateless]
  - decisions `(kindSort, projectId, environmentId, rankSort, reasonCode, releaseId, path)` [Source: docs/inputs/requirements_source.md#API-REQ-0008-—-Deterministic-DiagnosticsDecision-Log]
