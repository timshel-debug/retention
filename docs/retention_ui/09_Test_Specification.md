# Comprehensive Test Specification (UI + Integration)

This specification defines **unit**, **component**, **integration**, and **end-to-end** test coverage for the Release Retention Console (React + TypeScript), including cross-system tests against the API.

Scope drivers:
- Dataset entry (paste/upload) and deterministic client-side validation. [Source: docs/inputs/requirements_source.md#UI-REQ-0001-—-Paste-Dataset-JSON] [Source: docs/inputs/requirements_source.md#UI-REQ-0003-—-Client-Side-Validation]
- Server validation/evaluation calls and ProblemDetails error UX. [Source: docs/inputs/requirements_source.md#UI-REQ-0004-—-Server-Side-Validation] [Source: docs/inputs/requirements_source.md#UI-REQ-0010-—-Error-UX-ProblemDetails]
- Deterministic rendering and stable sorting/tie-breakers. [Source: docs/inputs/requirements_source.md#UI-NFR-0004-—-Determinism]
- E2E flows: upload → validate → evaluate → export. [Source: docs/inputs/requirements_source.md#8.-Testing-Requirements-UI]

---

## 1. Test Levels and Tooling

### 1.1 Unit Tests (pure)
**Goal:** correctness + determinism of utilities and validators.

- Runner: Jest/Vitest (implementation choice).
- Focus:
  - stable sort utilities
  - JSON path builder
  - client-side validation
  - ProblemDetails parsing and mapping
  - export helpers (stringify/download payload)
- No DOM required for most unit tests.

### 1.2 Component Tests (React Testing Library)
**Goal:** verify UI workflows, rendering, and state transitions in isolation with mocked API responses.

- Use RTL + user-event.
- Mock fetch (MSW recommended) to simulate:
  - success
  - ProblemDetails responses
  - network failures/timeouts

### 1.3 UI ↔ API Integration Tests (contract-driven)
**Goal:** verify the UI calls the real API endpoints and can render real responses without mocks.

- Use Playwright (preferred) or a dedicated integration runner.
- Starts API locally (test profile) and UI dev server (or built preview server).
- Validates:
  - request shapes
  - response parsing
  - error handling (ProblemDetails and network)

### 1.4 End-to-End (E2E) Tests
**Goal:** full flow works (user actions in browser, network to API, responses rendered, export actions work). [Source: docs/inputs/requirements_source.md#8.-Testing-Requirements-UI]

- Playwright tests cover the primary scenarios and failure modes.

---

## 2. Deterministic Fixtures

### 2.1 Canonical UI Fixtures
- `Fixture-A.json`: minimal valid dataset
- `Fixture-B.json`: multi project/env; includes tie conditions for stable sort tie-breakers
- `Fixture-C-invalid.json`: invalid dataset (missing fields / null elements) for validation UIs

### 2.2 Expected Response Fixtures (for mocked tests)
- `validate_ok.json`
- `validate_invalid.json`
- `evaluate_ok.json`
- `problem_validation_n_negative.json`
- `problem_rate_limited.json`

All fixtures MUST be stable text files committed to the repo and referenced by tests.

---

## 3. Test Cases (UI)

### 3.1 Dataset Input

#### UI-TST-0001 — Paste valid JSON populates parsed state
- **Requirement mapping:** REQ-0001. [Source: docs/inputs/requirements_source.md#UI-REQ-0001-—-Paste-Dataset-JSON]
- **Steps:** paste `Fixture-A.json`.
- **Assert:** no parse error; validate/evaluate actions become available (subject to other constraints).

#### UI-TST-0002 — Paste invalid JSON shows inline parse error
- **Requirement mapping:** REQ-0001.
- **Steps:** paste malformed JSON.
- **Assert:** inline error shown with actionable message; server validation/evaluate buttons disabled.

#### UI-TST-0003 — Upload .json file populates editor
- **Requirement mapping:** REQ-0002. [Source: docs/inputs/requirements_source.md#UI-REQ-0002-—-Upload-Dataset-JSON]
- **Steps:** upload Fixture-A.json.
- **Assert:** editor populated; no parse error.

#### UI-TST-0004 — Upload unsupported type rejected
- **Requirement mapping:** REQ-0002.
- **Steps:** upload `.txt`.
- **Assert:** clear error message; editor unchanged.

### 3.2 Client-Side Validation (Deterministic)

#### UI-TST-0101 — Client validation errors include stable paths
- **Requirement mapping:** REQ-0003. [Source: docs/inputs/requirements_source.md#UI-REQ-0003-—-Client-Side-Validation]
- **Setup:** parse Fixture-C-invalid.
- **Assert:** errors include stable JSON paths (e.g., `$.projects[0].projectId`); no missing path fields for applicable errors.

#### UI-TST-0102 — Client validation message ordering deterministic
- **Requirement mapping:** REQ-0003, NFR-0004. [Source: docs/inputs/requirements_source.md#UI-NFR-0004-—-Determinism]
- **Setup:** run validator multiple times; shuffle internal object field ordering where applicable.
- **Assert:** message ordering stable, sorted by `(code, path, message)` with ordinal comparison.

### 3.3 Server-Side Validation Panel

#### UI-TST-0201 — Validate calls POST /api/v1/datasets/validate and renders errors/warnings
- **Requirement mapping:** REQ-0004. [Source: docs/inputs/requirements_source.md#UI-REQ-0004-—-Server-Side-Validation]
- **Mocked steps:** click Validate, MSW returns `validate_invalid.json`.
- **Assert:** panel renders errors/warnings and summary counts; messages sorted deterministically.

#### UI-TST-0202 — Validate shows network failure distinctly from ProblemDetails
- **Requirement mapping:** REQ-0010. [Source: docs/inputs/requirements_source.md#UI-REQ-0010-—-Error-UX-ProblemDetails]
- **Mocked steps:** fetch throws.
- **Assert:** Error panel indicates network failure; no HTTP status displayed.

#### UI-TST-0203 — Validate shows ProblemDetails fields and hint
- **Requirement mapping:** REQ-0010.
- **Mocked steps:** API returns 400 with PD body.
- **Assert:** displays status + `error_code` + hint; includes trace/correlation IDs if present.

### 3.4 releasesToKeep Input

#### UI-TST-0301 — releasesToKeep enforces integer >= 0
- **Requirement mapping:** REQ-0005. [Source: docs/inputs/requirements_source.md#UI-REQ-0005-—-Configure-releasesToKeep]
- **Steps:** enter `-1`, `1.2`, `abc`.
- **Assert:** invalid states show inline feedback; evaluate disabled.

### 3.5 Evaluate Workflow and Results

#### UI-TST-0401 — Evaluate calls POST /api/v1/retention/evaluate and prevents concurrent submits
- **Requirement mapping:** REQ-0006. [Source: docs/inputs/requirements_source.md#UI-REQ-0006-—-Execute-Evaluation]
- **Mocked steps:** click Evaluate twice quickly.
- **Assert:** only one request sent; loading state visible; controls locked until completion.

#### UI-TST-0402 — Results table renders required columns and supports filter
- **Requirement mapping:** REQ-0007. [Source: docs/inputs/requirements_source.md#UI-REQ-0007-—-Display-Kept-Releases]
- **Mocked setup:** return `evaluate_ok.json`.
- **Assert:** columns: Project, Environment, ReleaseId, Version, Created, LatestDeployedAt, Rank, ReasonCode; filters work; stable ordering on ties (explicit tie-breaker).

#### UI-TST-0403 — Decision log tab renders diagnostics distinct and filterable
- **Requirement mapping:** REQ-0008. [Source: docs/inputs/requirements_source.md#UI-REQ-0008-—-Display-Decision-Log]
- **Assert:** diagnostics visually distinct; filter toggles kept/diagnostic.

#### UI-TST-0404 — UI does not invent timestamps
- **Requirement mapping:** REQ-0006. [Source: docs/inputs/requirements_source.md#UI-REQ-0006-—-Execute-Evaluation]
- **Setup:** API response without any timestamp field.
- **Assert:** UI displays no timestamp.

### 3.6 Export

#### UI-TST-0501 — Download JSON exports raw response payload
- **Requirement mapping:** REQ-0009. [Source: docs/inputs/requirements_source.md#UI-REQ-0009-—-Export-Results]
- **Assert:** download triggered with correct filename and content equals raw response payload (or stable stringify with documented rules).

#### UI-TST-0502 — Copy to clipboard success/failure feedback
- **Requirement mapping:** REQ-0009.
- **Assert:** shows success on resolved clipboard promise; error message on rejected promise.

---

## 4. Cross-System Tests (UI ↔ API)

These tests validate that the UI and API work together using the **real** API server.

### 4.1 Integration Test Harness Assumptions (TODO if repo differs)
- API runs locally at a configured base URL (e.g., `http://localhost:5001`).
- UI runs locally (e.g., `http://localhost:5173`).
- Test environment supports enabling/disabling auth and rate limiting for scenario coverage.

### 4.2 Integration Test Cases

#### XSYS-TST-0001 — Validate: UI → API → UI renders server validation
- **Requirement mapping:** UI REQ-0004, API validate contract. [Source: docs/inputs/requirements_source.md#UI-REQ-0004-—-Server-Side-Validation]
- **Steps:** load UI, upload Fixture-C-invalid, click Validate.
- **Assert:** UI renders server errors/warnings; ordering stable across multiple runs.

#### XSYS-TST-0002 — Evaluate: UI → API → UI renders kept releases and decisions
- **Requirement mapping:** UI REQ-0006/0007/0008; API evaluate contract. [Source: docs/inputs/requirements_source.md#UI-REQ-0007-—-Display-Kept-Releases]
- **Steps:** upload Fixture-B, set releasesToKeep, click Evaluate.
- **Assert:** results tables render correct counts and stable ordering; decision log shows stable reason codes.

#### XSYS-TST-0003 — Error contract: negative releasesToKeep surfaces PD error_code and hint
- **Requirement mapping:** UI REQ-0010. [Source: docs/inputs/requirements_source.md#UI-REQ-0010-—-Error-UX-ProblemDetails]
- **Steps:** set releasesToKeep -1, click Evaluate.
- **Assert:** UI displays status + `validation.n_negative` + trace id.

#### XSYS-TST-0004 — Rate limit scenario (when enabled) returns 429 and UI displays rate_limited
- **Requirement mapping:** UI REQ-0010. [Source: docs/inputs/requirements_source.md#UI-REQ-0010-—-Error-UX-ProblemDetails]
- **Steps:** enable API rate limiting; run Evaluate repeatedly via scripted Playwright.
- **Assert:** UI displays 429 ProblemDetails and `error_code=rate_limited`.

---

## 5. End-to-End (E2E) Scenarios (Playwright)

### E2E-0001 — Happy path: upload → client validate → server validate → evaluate → export
- **Requirement mapping:** REQ-0002..0009. [Source: docs/inputs/requirements_source.md#8.-Testing-Requirements-UI]
- **Asserts:** no errors; export completes; tables stable order.

### E2E-0002 — Parse error: paste invalid JSON disables actions
- **Requirement mapping:** REQ-0001.

### E2E-0003 — Server error path: PD is rendered with status + error_code + hint
- **Requirement mapping:** REQ-0010.

### E2E-0004 — Network failure path: UI displays network error distinct from PD
- **Requirement mapping:** REQ-0010.

---

## 6. Traceability Matrix (UI)

| Requirement | Test cases |
|---|---|
| REQ-0001 | UI-TST-0001, 0002 |
| REQ-0002 | UI-TST-0003, 0004 |
| REQ-0003 | UI-TST-0101, 0102 |
| REQ-0004 | UI-TST-0201, XSYS-TST-0001 |
| REQ-0005 | UI-TST-0301 |
| REQ-0006 | UI-TST-0401, 0404, XSYS-TST-0002 |
| REQ-0007 | UI-TST-0402, XSYS-TST-0002 |
| REQ-0008 | UI-TST-0403, XSYS-TST-0002 |
| REQ-0009 | UI-TST-0501, 0502, E2E-0001 |
| REQ-0010 | UI-TST-0202, 0203, XSYS-TST-0003, XSYS-TST-0004, E2E-0003, E2E-0004 |
| NFR-0004 | UI-TST-0102, UI-TST-0402 |
