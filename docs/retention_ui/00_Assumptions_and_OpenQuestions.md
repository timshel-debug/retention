# Assumptions and Open Questions (UI)

## Assumptions

1. Single-page operator console with no accounts/persistence. [Source: docs/inputs/requirements_source.md#2.-Scope]
2. API implements required v1 endpoints and ProblemDetails error model. [Source: docs/inputs/requirements_source.md#UI-REQ-0004-—-Server-Side-Validation]
3. UI determinism is enforced via explicit stable sort keys and tie-breakers. [Source: docs/inputs/requirements_source.md#UI-NFR-0004-—-Determinism]

## Open Questions

1. Exact dataset schema and required fields. TODO. Impact: client-side validation may require adjustment. [Source: docs/inputs/requirements_source.md#UI-REQ-0003-—-Client-Side-Validation]
2. API base URL/CORS rules. TODO. Impact: runtime config/proxy. [Source: docs/inputs/requirements_source.md#UI-REQ-0004-—-Server-Side-Validation]
