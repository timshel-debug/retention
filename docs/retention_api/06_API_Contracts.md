# API Contracts (v1)

See `docs/inputs/requirements_source.md#7.-API-Contracts` for contract expectations. This DSDS prescribes DTO shapes and deterministic ordering rules.

## Evaluate

- `POST /api/v1/retention/evaluate`
- Request: `{ dataset, releasesToKeep>=0, correlationId? }`
- Response: `{ keptReleases[], decisions[], diagnostics }`
- Ordering: stable (documented in code + tests). [Source: docs/inputs/requirements_source.md#API-REQ-0001-—-Evaluate-Retention-stateless]

## Validate

- `POST /api/v1/datasets/validate`
- Response: `{ isValid, errors[], warnings[], summary }`
- Ordering: stable `(code, path, message)` [Source: docs/inputs/requirements_source.md#API-REQ-0002-—-Validate-Dataset]

## Errors

- RFC7807 ProblemDetails, `application/problem+json` with extensions:
  - `error_code`, `trace_id`, optional `correlation_id` [Source: docs/inputs/requirements_source.md#7.4-Error-Response]
