# Copilot Implementation Prompt (API)

Recommended model: **claude-sonnet** — best for multi-project .NET + tests + middleware correctness.

Copy/paste prompt below into Copilot Chat.

---

## Prompt

Implement the **Release Retention Service API** as a greenfield .NET solution, strictly following DSDS docs in this folder.

### Files to treat as source-of-truth

- `docs/inputs/requirements_source.md`
- `docs/02_Requirements.md`
- `docs/03_Architecture.md`
- `docs/06_API_Contracts.md`
- `docs/09_Test_Strategy_and_Gates.md`
- `docs/09_Test_Specification.md`
- `docs/10_Implementation_Plan.md`

### Hard rules

- Clean Architecture: `Retention.Api`, `Retention.Application`, `Retention.Domain`, `Retention.Infrastructure` (optional).
- Determinism: normalize inputs + explicit stable sorts; do not rely on list order or dictionary iteration order.
- Exception handling:
  - Single global middleware to produce RFC7807 ProblemDetails.
  - No stack traces or sensitive details in responses.
  - Always include stable `error_code` + `trace_id` + optional `correlation_id`.
  - Log once per failure at boundary (structured logs only).
- No persistence in v1.
- Add OpenAPI/Swagger and keep it current.
- Add OTel-friendly instrumentation and at least one ActivityListener-based test.

### Step-by-step implementation (do in order)

1. Create solution + projects:
   - `src/Retention.Api`
   - `src/Retention.Application`
   - `src/Retention.Domain`
   - `src/Retention.Infrastructure` (optional)
   - `tests/Retention.Api.IntegrationTests`
   - `tests/Retention.UnitTests`

2. API skeleton:
   - Controllers for:
     - `POST /api/v1/datasets/validate`
     - `POST /api/v1/retention/evaluate`
   - Health endpoints:
     - `GET /health/live`
     - `GET /health/ready`
   - Swagger/OpenAPI enabled (dev only UI).

3. DTOs/contracts:
   - Implement request/response DTOs per `docs/06_API_Contracts.md` and `requirements_source.md#7`.
   - Ensure JSON property names match exactly.

4. Validation + normalization:
   - Boundary validation: `releasesToKeep >= 0` -> 400 PD `error_code=validation.n_negative`.
   - Deterministic validation messages sorted `(code, path, message)` (ordinal compare).
   - Normalize dataset arrays by stable keys before validate/evaluate.

5. Domain evaluator port:
   - Define `IRetentionEvaluator` + result types (keptReleases, decisions, diagnostics).
   - Implement a deterministic placeholder evaluator that selects top N per (project, environment) with stable ties.
   - Use stable reason codes: `kept.top_n`, `diagnostic.invalid_reference`.

6. Middleware:
   - Global exception middleware -> ProblemDetails mapping.
   - Always include `trace_id` and pass correlation id through.

7. Optional toggles:
   - Configurable JWT auth (disabled by default).
   - Configurable rate limiting (disabled by default); exceed -> 429 PD `error_code=rate_limited`.

8. Observability:
   - Add spans around validate/evaluate with attributes in `requirements_source.md#API-NFR-0003`.
   - Add a unit test using ActivityListener to verify span names + key attributes.

9. Tests:
   - Integration tests:
     - health endpoints 200
     - negative releasesToKeep -> 400 PD + `validation.n_negative`
     - determinism: shuffled inputs -> identical output JSON ordering
     - rate limit 429 PD when enabled (use small test limit)
     - auth 401 when enabled and token missing/invalid
   - Contract test: OpenAPI includes endpoints and ProblemDetails schema.

### Acceptance criteria

- `dotnet build` and `dotnet test` succeed.
- Determinism tests prove stable ordering.
- ProblemDetails shape matches DSDS and never includes stack traces.
- OpenAPI is present and describes v1 endpoints and DTOs.
