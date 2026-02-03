# Architecture (UI)

## Structure

- React functional components + hooks; TypeScript throughout. [Source: docs/inputs/requirements_source.md#3.-Design-Constraints-and-Standards]
- Feature folders:
  - `features/dataset`
  - `features/validation`
  - `features/evaluation`
  - `features/export`
  - `shared/*` (types, api client, sorting, components)

## Determinism policy

- Explicit stable sort utilities with tie-breakers for every list. [Source: docs/inputs/requirements_source.md#UI-NFR-0004-—-Determinism]
- Validation messages sorted `(code, path, message)` deterministically. [Source: docs/inputs/requirements_source.md#UI-REQ-0003-—-Client-Side-Validation]

## Error handling

- API client returns discriminated union: `ok | problem | network`.
- ProblemDetails displayed with status + `error_code` + hint + trace/correlation IDs. [Source: docs/inputs/requirements_source.md#UI-REQ-0010-—-Error-UX-ProblemDetails]
- Do not invent evaluation timestamp; display only if present. [Source: docs/inputs/requirements_source.md#UI-REQ-0006-—-Execute-Evaluation]
