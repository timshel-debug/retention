# Report: How to use the Solution Design Prompt to specify requirements for the Release Retention solution

## 1) Objective

Use the solution design prompt as a deterministic procedure for turning the provided domain brief + sample data into an auditable requirements set (REQ/NFR) with explicit acceptance criteria, traceability, and a controlled set of assumptions/open questions.

For this solution, the *source-of-truth* is the exercise brief plus the supplied JSON datasets. The core behavior is the “Release Retention rule” and the implementation constraints stated in the task description. [Source: Start Here - Instructions - Release Retention.md:L25-L42]

---

## 2) Inputs and what they contribute

The prompt starts by forcing an explicit inventory of inputs (so you can prove you didn’t hallucinate requirements).

### 2.1 Normative behavior and constraints (the brief)

The “Task” section contains the hard requirements:

* Compute releases to keep from **projects/environments/releases/deployments**. [Source: Start Here - Instructions - Release Retention.md:L27-L34]
* Retention rule: per **project/environment** keep `n` releases with the most recent deployments; “deployed” means at least one deployment exists. [Source: Start Here - Instructions - Release Retention.md:L29-L34]
* Constraints: reusable/testable; include tests; **no UI/CLI/database**; `n` is a parameter; return kept releases; log why a release was kept. [Source: Start Here - Instructions - Release Retention.md:L35-L41]

The “Success” section provides the business motivation that influences NFRs (scale/performance and storage pressure): logs/artifacts storage, findability, and slow processing at high release counts. [Source: Start Here - Instructions - Release Retention.md:L13-L17]

### 2.2 Domain vocabulary (concept definitions)

The prompt uses the “DevOps Deploy Concepts” section to lock the ubiquitous language (Project/Release/Environment/Deployment) so requirements use consistent nouns and don’t invent new ones. [Source: Start Here - Instructions - Release Retention.md:L108-L124]

### 2.3 Data shape (sample JSON)

The JSON files define the expected input schema at a practical level:

* Projects: `{ Id, Name }` [Source: Projects.json:L1-L10]
* Environments: `{ Id, Name }` [Source: Environments.json:L1-L10]
* Releases: `{ Id, ProjectId, Version, Created }` (note one sample has `Version: null`) [Source: Releases.json:L1-L19]
* Deployments: `{ Id, ReleaseId, EnvironmentId, DeployedAt }` and the sample includes multiple deployments for the same release/environment. [Source: Deployments.json:L31-L55]

These shapes become “input contract” requirements and validation requirements.

---

## 3) How the prompt yields Greenfield 


This brief asks you to “implement the Release Retention rule” as a standalone deliverable, with explicit constraints including no UI/CLI/DB, and no references to an existing codebase to modify. That is a greenfield implementation target. [Source: Start Here - Instructions - Release Retention.md:L35-L42]

---

## 4) The mechanics: converting sources into REQ/NFR

The prompt is effectively a “requirements compiler”. You apply it in a strict sequence so the output is deterministic and auditable.

### Step 1 — Extract explicit requirements (verbatim → normalized)

You identify every statement that uses directive language (“Your implementation should…”, “The rule…”, “Keep n…”). For each statement:

1. Convert it into a single requirement sentence.
2. Assign a stable ID (REQ/NFR).
3. Add acceptance criteria that can be tested.
4. Attach the evidence tag.

Example (directly from the task text):

* Requirement sentence: “The system shall take the number of releases to keep (`n`) as an input parameter.”
* Evidence: [Source: Start Here - Instructions - Release Retention.md:L39-L40]

### Step 2 — Derive implied requirements *only* when forced by the brief/data

The prompt allows derivation but only when it’s required to make the explicit behavior implementable and testable.

For this solution, derivations are forced by the combination of the rule + sample data:

* Multiple deployments can exist for a release/environment pair (sample shows `Release-6` deployed twice to the same environment). That forces a requirement to define “most recently deployed” as “max(DeployedAt) per release/environment”. [Source: Deployments.json:L31-L55]
* A release can have “zero or more deployments” and is “considered deployed if it has one or more deployments”, forcing a requirement for how to treat releases with zero deployments (they cannot be selected as “most recently deployed”). [Source: Start Here - Instructions - Release Retention.md:L29-L34]
* Release `Version` may be null in provided data, forcing a requirement around null-handling in logging and outputs (no crashes; stable behavior). [Source: Releases.json:L14-L19]

Any other “nice to have” is not a requirement; it becomes either a TODO or an “Ideas and Improvements” note. [Source: Start Here - Instructions - Release Retention.md:L100-L103]

### Step 3 — Capture ambiguities as assumptions/open questions with impact

Where the sources don’t define behavior (tie-breaks, invalid references, ordering), the prompt requires you to proceed with a best-effort default and record it as an_professional assumption_ including:

* what’s missing,
* what default you chose,
* why it’s safe,
* and the impact if wrong.

That aligns with the brief’s explicit instruction to make assumptions rather than being blocked. [Source: Start Here - Instructions - Release Retention.md:L96-L99]

---

## 5) Requirements excerpt produced by applying the prompt to this solution

This is what your prompt-driven requirements spec looks like in practice (not the full DSDS; this is the part the prompt produces for “02_Requirements.md”).

### Functional requirements (REQ)

**REQ-0001 — Accept input datasets**
The system shall accept as input a set of Projects, Environments, Releases, and Deployments with the shapes demonstrated in the supplied JSON files.
Acceptance criteria:

* Parsing succeeds for the provided sample datasets.
* IDs and referenced IDs are accessible to the evaluator.
  [Source: Start Here - Instructions - Release Retention.md:L27-L28] [Source: Projects.json:L1-L10] [Source: Environments.json:L1-L10] [Source: Releases.json:L1-L19] [Source: Deployments.json:L1-L12]

**REQ-0002 — Evaluate retention per project/environment**
For each project/environment combination, the system shall select `n` releases with the most recent deployments.
Acceptance criteria:

* Given the same inputs and `n`, output is stable and matches the rule.
* Selection is computed independently for each project/environment.
  [Source: Start Here - Instructions - Release Retention.md:L32-L34]

**REQ-0003 — Define “deployed”**
A release shall be eligible for selection only if it has one or more deployments.
Acceptance criteria:

* Releases with zero deployments are never returned as “kept”.
  [Source: Start Here - Instructions - Release Retention.md:L33-L34]

**REQ-0004 — Define “most recently deployed” per release/environment**
Where a release has multiple deployments to the same environment, “most recently deployed” shall be computed using the maximum `DeployedAt` for that release/environment pair.
Acceptance criteria:

* For a release with multiple deployments to the same environment, the evaluator uses the latest timestamp.
  [Source: Start Here - Instructions - Release Retention.md:L30-L33] [Source: Deployments.json:L31-L55]

**REQ-0005 — Parameterize by `n`**
The system shall take `n` (releases to keep) as an input parameter.
Acceptance criteria:

* `n` can be varied in tests and changes the result set as expected.
  [Source: Start Here - Instructions - Release Retention.md:L39-L40]

**REQ-0006 — Return kept releases**
The system shall return the releases that should be kept.
Acceptance criteria:

* Output includes release identifiers sufficient to correlate back to the input releases.
  [Source: Start Here - Instructions - Release Retention.md:L40-L41]

**REQ-0007 — Log keep reasons**
The system shall log why a release was kept.
Acceptance criteria:

* For each kept release, a log entry exists indicating it was kept due to being within the top `n` most recently deployed for a project/environment.
  [Source: Start Here - Instructions - Release Retention.md:L41-L42]

### Non-functional requirements (NFR)

**NFR-0001 — Reusable and testable design**
The solution shall be structured to be reusable and testable.
Acceptance criteria:

* Core retention logic is callable without UI/CLI/database dependencies.
  [Source: Start Here - Instructions - Release Retention.md:L35-L38]

**NFR-0002 — Test suite present**
The solution shall include tests consistent with production expectations.
Acceptance criteria:

* Includes automated tests covering the rule and edge cases (e.g., multiple deployments, no deployments, null version).
  [Source: Start Here - Instructions - Release Retention.md:L36-L38] [Source: Releases.json:L14-L19] [Source: Deployments.json:L31-L55]

**NFR-0003 — No UI/CLI/DB**
The solution shall not depend on a UI, CLI, or database.
Acceptance criteria:

* Build artifacts and runtime do not require an interactive front-end or a database connection.
  [Source: Start Here - Instructions - Release Retention.md:L38-L39]

**NFR-0004 — Scale-aware performance intent**
The solution shall handle large numbers of releases/deployments without pathological slowdowns in the evaluator.
Acceptance criteria:

* Algorithmic complexity documented and bounded (e.g., sorting/grouping per project/environment).
  [Source: Start Here - Instructions - Release Retention.md:L13-L17]

---

## 6) How you apply your open-question corrections (Q-0001…Q-0005) when using the prompt

When you run the prompt, you *still* produce an “Assumptions/Open Questions” section, but you prune and constrain it using your corrections:

* **Q-0001: remove it**
  If a question is unnecessary because the sources already decide it or it’s not blocking the solution, you delete it rather than leaving noise. (This aligns with determinism: fewer ambiguous branches.)

* **Q-0002: scope to the specific environment being evaluated**
  Even though the rule is defined per project/environment combination, you constrain the execution context for this solution to evaluate only the targeted environment in a given run (a valid restriction of the general rule), and you document that as an assumption with rationale and impact if the caller expects global evaluation. [Source: Start Here - Instructions - Release Retention.md:L32-L34]

* **Q-0003: stick with current design**
  Where the brief explicitly dictates outputs (return kept releases; log reasons; no UI/CLI/DB), you do not reopen those as questions. [Source: Start Here - Instructions - Release Retention.md:L38-L42]

* **Q-0004: include reasoning**
  For every remaining assumption, you include the missing info, the chosen default, and the impact. This is directly encouraged by the brief’s “make assumptions rather than being blocked” instruction. [Source: Start Here - Instructions - Release Retention.md:L96-L99]

* **Q-0005: coordinate deletion of related logs and artifacts**
  The business problem explicitly calls out logs/artifacts storage pressure, but the input data does not include artifact identifiers. So when you use the prompt you:

  * introduce a requirement/assumption that retention *decisions* must be consumable by a downstream deletion process, and
  * record a TODO that artifact/log linkage is unknown in the provided inputs, with impact (cannot guarantee complete cleanup without additional data).
    [Source: Start Here - Instructions - Release Retention.md:L15-L17]

---

## 7) Why this approach is effective (and auditable)

Using the solution design prompt this way guarantees:

* **Traceability**: every REQ/NFR is grounded in a cited source line.
* **Determinism**: stable IDs + explicitly recorded assumptions prevent “design drift”.
* **Implementation readiness**: acceptance criteria are testable statements derived from the rule and data shapes.
* **Constraint compliance**: the “no UI/CLI/DB”, parameterization, and logging requirements are captured as first-class requirements, not afterthoughts. [Source: Start Here - Instructions - Release Retention.md:L35-L42]

That is the practical method: treat the prompt as a strict transformation pipeline from inputs → requirements, with ambiguity handled via explicitly documented assumptions rather than speculative feature invention.
