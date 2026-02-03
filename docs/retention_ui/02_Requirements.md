# Requirements (UI)

This DSDS reflects only the **UI** requirements in `docs/inputs/requirements_source.md`.

## Functional requirements

| ID | Name | Summary | Source |
|---|---|---|---|
| REQ-0001 | Paste dataset JSON | Text editor accepts pasted JSON and shows parse errors inline. | [Source: docs/inputs/requirements_source.md#UI-REQ-0001-—-Paste-Dataset-JSON] |
| REQ-0002 | Upload dataset JSON | Upload `.json` file to populate editor; reject unsupported types. | [Source: docs/inputs/requirements_source.md#UI-REQ-0002-—-Upload-Dataset-JSON] |
| REQ-0003 | Client-side validation | Local required/type errors with stable paths; deterministic ordering. | [Source: docs/inputs/requirements_source.md#UI-REQ-0003-—-Client-Side-Validation] |
| REQ-0004 | Server-side validation | Call API `POST /api/v1/datasets/validate` and display errors/warnings. | [Source: docs/inputs/requirements_source.md#UI-REQ-0004-—-Server-Side-Validation] |
| REQ-0005 | Configure releasesToKeep | Numeric input constrained to integer >= 0 with inline feedback. | [Source: docs/inputs/requirements_source.md#UI-REQ-0005-—-Configure-releasesToKeep] |
| REQ-0006 | Execute evaluation | Call API `POST /api/v1/retention/evaluate`, show loading, prevent concurrent submits, do not invent timestamps. | [Source: docs/inputs/requirements_source.md#UI-REQ-0006-—-Execute-Evaluation] |
| REQ-0007 | Display kept releases | Table with required columns + filters; stable ordering with tie-breakers. | [Source: docs/inputs/requirements_source.md#UI-REQ-0007-—-Display-Kept-Releases] |
| REQ-0008 | Display decision log | Separate view; diagnostics visually distinct; filterable. | [Source: docs/inputs/requirements_source.md#UI-REQ-0008-—-Display-Decision-Log] |
| REQ-0009 | Export results | Download JSON response and copy to clipboard with feedback. | [Source: docs/inputs/requirements_source.md#UI-REQ-0009-—-Export-Results] |
| REQ-0010 | Error UX | Display ProblemDetails fields with hints; network errors distinct. | [Source: docs/inputs/requirements_source.md#UI-REQ-0010-—-Error-UX-ProblemDetails] |

## Non-functional requirements

| ID | Name | Summary | Source |
|---|---|---|---|
| NFR-0001 | Accessibility | Keyboard navigation, focus visibility, labelled controls, semantic tables. | [Source: docs/inputs/requirements_source.md#UI-NFR-0001-—-Accessibility] |
| NFR-0002 | Performance | Avoid unnecessary re-renders; memoize derived views. | [Source: docs/inputs/requirements_source.md#UI-NFR-0002-—-Performance] |
| NFR-0003 | Maintainability | Feature folders; separate data-fetching from presentation components. | [Source: docs/inputs/requirements_source.md#UI-NFR-0003-—-Maintainability] |
| NFR-0004 | Determinism | Stable sorting/filtering; no reliance on object-key iteration order. | [Source: docs/inputs/requirements_source.md#UI-NFR-0004-—-Determinism] |
| NFR-0005 | Security | No payload logs in production; avoid dangerouslySetInnerHTML. | [Source: docs/inputs/requirements_source.md#UI-NFR-0005-—-Security] |
| NFR-0006 | Quality gates | ESLint/Prettier in CI; unit/component/E2E tests. | [Source: docs/inputs/requirements_source.md#UI-NFR-0006-—-Quality-Gates] |
