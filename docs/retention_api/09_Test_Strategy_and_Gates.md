# Test Strategy (API)

- Unit: sorting/normalization, ProblemDetails mapping.
- Integration: determinism with shuffled inputs, validation failures, health endpoints. [Source: docs/inputs/requirements_source.md#8.-Testing-Requirements-API]
- Contract: OpenAPI schema checks.

Gates:
- `dotnet build`
- `dotnet test`
- OpenAPI contract drift check (snapshot or schema validation). [Source: docs/inputs/requirements_source.md#API-REQ-0004-—-OpenAPI-Definition]
