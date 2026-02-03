# Overview (API)

API exposes two primary operations:
- Validate dataset: `POST /api/v1/datasets/validate` [Source: docs/inputs/requirements_source.md#API-REQ-0002-—-Validate-Dataset]
- Evaluate retention: `POST /api/v1/retention/evaluate` [Source: docs/inputs/requirements_source.md#API-REQ-0001-—-Evaluate-Retention-stateless]

Cross-cutting:
- RFC7807 ProblemDetails error contract [Source: docs/inputs/requirements_source.md#API-REQ-0007-—-Error-Handling-via-ProblemDetails]
- Deterministic ordering policy [Source: docs/inputs/requirements_source.md#API-REQ-0001-—-Evaluate-Retention-stateless]
- OpenAPI/Swagger [Source: docs/inputs/requirements_source.md#API-REQ-0004-—-OpenAPI-Definition]
