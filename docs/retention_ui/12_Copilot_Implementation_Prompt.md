# Copilot Implementation Prompt (UI)

Recommended model: **claude-sonnet** — best for React+TS structure plus test scaffolding (RTL + Playwright).

Copy/paste prompt below into Copilot Chat.

---

## Prompt

Implement the **Release Retention Console UI** (React + TypeScript) as a greenfield app, strictly following DSDS docs in this folder.

### Files to treat as source-of-truth

- `docs/inputs/requirements_source.md`
- `docs/02_Requirements.md`
- `docs/03_Architecture.md`
- `docs/06_API_Contracts.md`
- `docs/09_Test_Strategy_and_Gates.md`
- `docs/09_Test_Specification.md`
- `docs/10_Implementation_Plan.md`

### Hard rules

- React functional components + hooks; TypeScript everywhere. [Source: docs/inputs/requirements_source.md#3.-Design-Constraints-and-Standards]
- CSS Modules (or equivalent) for non-trivial styling; semantic HTML first.
- Determinism:
  - explicit stable sorting utilities with tie-breakers; no reliance on object-key iteration order. [Source: docs/inputs/requirements_source.md#UI-NFR-0004-—-Determinism]
- Security:
  - avoid `dangerouslySetInnerHTML` and do not log dataset payloads in production builds. [Source: docs/inputs/requirements_source.md#UI-NFR-0005-—-Security]
- Tests required: unit + RTL component + Playwright E2E. [Source: docs/inputs/requirements_source.md#8.-Testing-Requirements-UI]

### Project layout (must match)

- `src/app/`
- `src/features/dataset/`
- `src/features/validation/`
- `src/features/evaluation/`
- `src/features/export/`
- `src/shared/api/`
- `src/shared/types/`
- `src/shared/utils/`
- `src/shared/components/`
- `tests/e2e/`

### Step-by-step implementation (do in order)

1. Scaffold React + TS app (tooling of choice).
   - Configure ESLint + Prettier.
   - Add scripts: `lint`, `format:check`, `typecheck`, `test`, `test:e2e`.
   - Add React Testing Library and Playwright.

2. Types + deterministic utilities:
   - Define TS types for Dataset, ValidationMessage, ValidationResponse, EvaluationResponse, ProblemDetails.
   - Implement stable sort helper with explicit tie-breakers.
   - Implement JSON path helper producing stable paths (e.g., `$.projects[0].projectId`).

3. Dataset editor (REQ-0001):
   - Textarea editor with inline parse error.
   - Store raw text + parsed dataset (nullable).

4. File upload (REQ-0002):
   - Accept only `.json`.
   - On load, populate editor text.
   - Reject others with clear message.

5. Client-side validation (REQ-0003):
   - Implement `validateDatasetClient(dataset)` for structural checks and best-effort required fields/references.
   - Return errors/warnings deterministically sorted `(code, path, message)`.

6. API client + error model (REQ-0004/REQ-0006/REQ-0010):
   - Implement `ApiClient.validateDataset` and `ApiClient.evaluateRetention` for:
     - `POST /api/v1/datasets/validate`
     - `POST /api/v1/retention/evaluate`
   - Return discriminated union: `ok | problem | network`.
   - Parse ProblemDetails including `error_code`, `trace_id`, optional `correlation_id`.

7. Server validation panel (REQ-0004):
   - Button triggers call; display errors/warnings/summary.
   - Show loading state.
   - Route errors to a shared `ErrorPanel`.

8. releasesToKeep input (REQ-0005):
   - Enforce integer >= 0 and disable evaluate when invalid.

9. Evaluate workflow + results (REQ-0006..REQ-0008):
   - Evaluate button with loading and submit lock.
   - Results tabs:
     - Kept releases table with required columns + filters.
     - Decision log table with kind filter; diagnostics visually distinct.
   - Apply deterministic stable sorting for all rendered lists, with tie-breakers.

10. Export (REQ-0009):
   - Download raw evaluation response JSON.
   - Copy to clipboard with feedback.
   - Preserve raw payload exactly if feasible; otherwise use stable stringify with sorted keys.

11. Tests:
   - Unit: stable sort + client validation determinism + ProblemDetails parsing.
   - Component: paste/upload flows; validate/evaluate; deterministic table order.
   - E2E (Playwright): upload → validate → evaluate → export; parse error; server vs network error flows.

### Acceptance criteria

- `npm run lint`, `npm run format:check`, `npm run typecheck`, `npm run test`, `npm run test:e2e` all succeed.
- Deterministic ordering verified by unit/component tests.
- No dataset payload logs in production build.
