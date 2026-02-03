# Test Strategy (UI)

- Unit tests: sorting utilities, client validation determinism, ProblemDetails parsing.
- Component tests (RTL): paste/upload, validate/evaluate flows, result tables/filtering.
- E2E (Playwright): upload → validate → evaluate → export; parse/server/network error flows. [Source: docs/inputs/requirements_source.md#8.-Testing-Requirements-UI]

Gates:
- ESLint + Prettier
- Typecheck
- Unit/component tests
- E2E tests in CI (separate job if needed). [Source: docs/inputs/requirements_source.md#UI-NFR-0006-—-Quality-Gates]
