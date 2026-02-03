# UI Requirements Specification — Release Retention Console (React + TypeScript)

## 1. Purpose

Provide a React + TypeScript web UI to import/prepare a dataset, validate it, run evaluation, and inspect/export results and diagnostics.

## 2. Scope

**In-scope**

* Dataset input (paste JSON, upload JSON file).
* Client-side validation + server validation.
* Configure `releasesToKeep`.
* Run evaluation and display kept releases and decision log.
* Export results (download JSON, copy to clipboard).
* Accessible, responsive UI with clear error states.

**Out of scope (unless later specified)**

* User accounts, persistence, collaboration.
* Executing coordinated deletion.

## 3. Design Constraints and Standards

* React functional components + hooks, TypeScript throughout.
* ESLint + Prettier configured and enforced.
* Avoid inline styles for non-trivial styling; use CSS Modules or equivalent.
* Semantic HTML5 first; ARIA only as needed.

## 4. Users

* **Operator**: runs evaluations and inspects outcomes.

---

## 5. Functional Requirements

### UI-REQ-0001 — Paste Dataset JSON

**Acceptance criteria**

1. JSON editor accepts pasted dataset.
2. Parse errors shown inline, with actionable message.

### UI-REQ-0002 — Upload Dataset JSON

**Acceptance criteria**

1. Upload `.json` file to populate editor.
2. Unsupported file types rejected with clear message.

### UI-REQ-0003 — Client-Side Validation

**Acceptance criteria**

1. Show required-field/type errors without server roundtrip.
2. Messages include a stable “path” to the offending element.
3. Messages are sorted deterministically.

### UI-REQ-0004 — Server-Side Validation

**Acceptance criteria**

1. Calls `POST /api/v1/datasets/validate`.
2. Displays returned errors/warnings in a dedicated panel.

### UI-REQ-0005 — Configure `releasesToKeep`

**Acceptance criteria**

1. Numeric input constrained to integer >= 0.
2. Inline feedback for invalid entry.

### UI-REQ-0006 — Execute Evaluation

**Acceptance criteria**

1. Calls `POST /api/v1/retention/evaluate`.
2. Shows loading state; prevents concurrent submits.
3. Displays evaluation timestamp only if returned by API (UI must not invent it).

### UI-REQ-0007 — Display Kept Releases

**Acceptance criteria**

1. Table view with columns: Project, Environment, ReleaseId, Version, Created, LatestDeployedAt, Rank, ReasonCode.
2. Sort by Rank and filter by Project/Environment.
3. Stable ordering when sort keys tie (explicit tie-breaker).

### UI-REQ-0008 — Display Decision Log

**Acceptance criteria**

1. Separate tab/view for decisions.
2. Diagnostics visually distinct; filterable (kept/diagnostic).

### UI-REQ-0009 — Export Results

**Acceptance criteria**

1. Download JSON of the raw response payload.
2. Copy raw JSON to clipboard with success/failure feedback.

### UI-REQ-0010 — Error UX (ProblemDetails)

**Acceptance criteria**

1. Server errors display: HTTP status + `error_code` + user-action hint.
2. Network failures handled distinctly from 4xx/5xx server errors.

---

## 6. Non-Functional Requirements

### UI-NFR-0001 — Accessibility

* Keyboard navigation, focus visibility, labelled controls, proper table semantics.

### UI-NFR-0002 — Performance

* Avoid unnecessary re-renders for large payloads; memoize derived views (sort/filter).
* No O(n²) UI transformations on large lists unless unavoidable.

### UI-NFR-0003 — Maintainability

* Feature-folder structure (e.g., `features/dataset`, `features/evaluation`).
* Separate data-fetching and pure presentation components.

### UI-NFR-0004 — Determinism

* UI sorting/filtering is stable and does not rely on object-key iteration order.

### UI-NFR-0005 — Security

* Do not log dataset payloads in production builds.
* Avoid `dangerouslySetInnerHTML`.

### UI-NFR-0006 — Quality Gates

* Lint/format checks in CI.
* Unit/component/E2E test suites.

---

## 7. Screen Model (Information Architecture)

Single-page console (or simple router) with:

1. Dataset Input: Paste / Upload
2. Validation: Client + Server
3. Parameters: releasesToKeep
4. Results: Kept Releases / Decision Log
5. Export actions

---

## 8. Testing Requirements (UI)

* Unit tests: validation helpers, ProblemDetails mapping, stable sorting.
* Component tests (React Testing Library): dataset workflows, results rendering.
* E2E (Playwright): upload → validate → evaluate → export; parse error and server-error flows.
