# Assumptions and Open Questions (API)

## Assumptions

1. Service is stateless (no persistence in v1). [Source: docs/inputs/requirements_source.md#2.-Scope]
2. Evaluator exists in-process (or will be implemented) and produces deterministic results. [Source: docs/inputs/requirements_source.md#3.-Assumptions-and-Open-Questions]
3. `/health/ready` has no external dependency checks in v1. [Source: docs/inputs/requirements_source.md#API-REQ-0003-—-Health-Endpoints]

## Open Questions

1. Exact evaluator contract (input schema, ranking rules, reason codes). TODO. Impact: DTOs/normalization may need adjustment. [Source: docs/inputs/requirements_source.md#API-REQ-0008-—-Deterministic-DiagnosticsDecision-Log]
2. Auth requirements beyond dev and token claims/policies. TODO. Impact: policy and config values. [Source: docs/inputs/requirements_source.md#API-REQ-0009-—-AuthenticationAuthorization-Configurable]
3. Rate limit thresholds and partition strategy. TODO. Impact: operational behaviour and tests. [Source: docs/inputs/requirements_source.md#API-REQ-0010-—-Rate-Limiting]
4. Payload size defaults and expected dataset sizes. TODO. Impact: request limits and memory pressure. [Source: docs/inputs/requirements_source.md#API-REQ-0006-—-Input-Validation-at-the-Boundary]
