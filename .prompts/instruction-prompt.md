You are VS Code Copilot implementing a .NET coding exercise solution using the DSDS in docs/** as the single source of truth.

STRICT SCOPE
- Implement ONLY code + tests + README needed to satisfy the DSDS requirements in docs/02_Requirements.md, docs/03_Architecture.md, docs/04_Domain_Model.md, docs/05_Data_Model.md, docs/06_API_Contracts.md, docs/10_Implementation_Plan.md, and docs/14_Test_Specification.md (if present).
- Do NOT modify docs/** (except README.md at repo root which is a deliverable).
- Do NOT add a CLI/UI/DB.
- Determinism is mandatory: no DateTime.Now/Guid.NewGuid in core logic, no randomness, stable ordering everywhere.

DELIVERABLES (STRUCTURE)
Create a solution with Clean Architecture layering:
- src/Retention.Domain/Retention.Domain.csproj
- src/Retention.Application/Retention.Application.csproj
- (optional) src/Retention.Infrastructure/Retention.Infrastructure.csproj (only if you introduce host adapters; keep empty otherwise)
- tests/Retention.UnitTests/Retention.UnitTests.csproj
- tests/Retention.IntegrationTests/Retention.IntegrationTests.csproj
- README.md (root)

IMPLEMENTATION STEPS

1) DOMAIN LAYER (src/Retention.Domain)
1.1 Entities (per docs/05_Data_Model.md):
- Project { string Id; string Name; }
- Environment { string Id; string Name; }
- Release { string Id; string ProjectId; string? Version; DateTimeOffset Created; }
- Deployment { string Id; string ReleaseId; string EnvironmentId; DateTimeOffset DeployedAt; }

1.2 Domain service:
- RetentionPolicyEvaluator
  Responsibilities:
  - Accept pre-validated inputs needed for policy evaluation.
  - Group deployments by (ProjectId, EnvironmentId, ReleaseId) and compute LatestDeployedAt = max(DeployedAt). (REQ-0004)
  - Determine eligibility: only releases with >=1 deployment to that environment are eligible. (REQ-0002)
  - Rank per (ProjectId, EnvironmentId) using tie-breakers from ADR-0003:
    1) LatestDeployedAt desc
    2) Release.Created desc
    3) Release.Id asc
  - Select top n per (ProjectId, EnvironmentId). (REQ-0003)
  - Output a deterministic ranked list with all data needed to build DTOs/decision logs.

1.3 Domain result types:
- A domain-level candidate record/struct holding:
  ProjectId, EnvironmentId, ReleaseId, Version, Created, LatestDeployedAt, Rank, ReasonCode
- Use stable ReasonCode values (string or enum). Minimum:
  - kept.top_n
  - diagnostic.invalid_reference (for REQ-0010 path; emitted by application layer if preferred)

2) APPLICATION LAYER (src/Retention.Application)
2.1 Public API (per docs/06_API_Contracts.md):
- Provide a single entry point:
  RetentionResult EvaluateRetention(
    IReadOnlyList<Project> projects,
    IReadOnlyList<Environment> environments,
    IReadOnlyList<Release> releases,
    IReadOnlyList<Deployment> deployments,
    int releasesToKeep,
    string? correlationId = null)

2.2 Validation and error taxonomy (REQ-0009, REQ-0010, docs/03_Architecture.md error strategy):
- Implement typed exceptions with stable codes:
  - ValidationException : Exception { string Code; }
  - DomainException : Exception { string Code; }
- Required validation:
  - releasesToKeep < 0 => throw ValidationException code "validation.n_negative" (REQ-0009)
  - Null element in any input list => throw ValidationException code "validation.null_element" (per docs/06; treat null collections as empty if desired, but null elements must fail deterministically)
- Invalid references handling (REQ-0010 + ADR-0005):
  - If a deployment references an EnvironmentId not in environments, or a ReleaseId not in releases, or a Release.ProjectId not in projects:
    - Exclude that deployment from eligibility calculations
    - Emit a diagnostic DecisionLogEntry with ReasonCode/ReasonText explaining invalid reference
    - Ensure diagnostics are deterministic and ordered

2.3 DTOs (per docs/05_Data_Model.md and docs/06_API_Contracts.md):
- RetentionResult:
  - IReadOnlyList<KeptRelease> KeptReleases (deterministic ordering)
  - IReadOnlyList<DecisionLogEntry> Decisions (includes kept + diagnostics)
  - RetentionDiagnostics Diagnostics (counts: invalid_deployments_excluded, groups_evaluated, etc.)
- KeptRelease:
  ReleaseId, ProjectId, EnvironmentId, Version, Created, LatestDeployedAt, Rank, ReasonCode
- DecisionLogEntry:
  ProjectId, EnvironmentId, ReleaseId, n, Rank, LatestDeployedAt, ReasonText, ReasonCode (add ReasonCode even if doc shows only text; keep stable)

2.4 Deterministic ordering rules (NFR-0003, REQ-0007):
- KeptReleases sorted by:
  ProjectId asc, EnvironmentId asc, Rank asc, ReleaseId asc
- Decisions sorted by:
  decision_type (kept before diagnostic) then ProjectId asc, EnvironmentId asc, Rank asc, ReleaseId asc
- CorrelationId:
  - If caller provides correlationId, copy into decision entries/diagnostics (as a field).
  - If absent, DO NOT generate random IDs in library. Use empty/null; host can supply.

2.5 Observability (optional addendum; NFR-0008, ADR-0007)
- Implement ActivitySource + Meter in Retention.Application only:
  - ActivitySource name "Retention"
  - Span "retention.evaluate" around EvaluateRetention
  - Add attributes: releases_to_keep (n), projects_count, environments_count, releases_count, deployments_count, invalid_deployments_excluded
- Do not add exporters or OpenTelemetry packages unless already referenced by repo; ActivitySource works without them.
- Instrumentation must not change behavior when no listener is configured.

3) TESTS

3.1 Unit tests (tests/Retention.UnitTests) — implement at minimum:
- REQ-0009: n < 0 throws ValidationException with Code="validation.n_negative"
- REQ-0002: release with zero deployments to env is not kept for that env
- REQ-0003/0004: latest deployment timestamp determines ranking
- ADR-0003 tie-breakers:
  - same LatestDeployedAt => Created desc wins
  - same LatestDeployedAt & Created => ReleaseId asc wins
- REQ-0006: multiple environments evaluated in one invocation (n=1 case yields kept for both envs)
- REQ-0010: invalid deployment references excluded and produce diagnostic decision entries; ensure kept set unaffected

Determinism tests:
- Shuffle input deployments and releases; output KeptReleases and Decisions identical (set + order). (NFR-0003)

3.2 Integration tests (tests/Retention.IntegrationTests)
- Contract test for EvaluateRetention:
  - Asserts DTO shapes, ordering, and stable reason codes
  - Includes multi-project, multi-env dataset
- If you implemented ActivitySource:
  - Attach an ActivityListener in test and assert one "retention.evaluate" activity emitted with expected attributes (no OpenTelemetry exporter required)

3.3 End-to-end tests (still in-process; no external services)
- Treat “E2E” as: load sample JSON inputs (Projects.json/Environments.json/Releases.json/Deployments.json) if they exist in repo, parse into entities, run EvaluateRetention, assert expected kept set for the provided sample scenarios in the exercise instructions.
- If the JSON files are not present, create a minimal embedded dataset matching the documented sample cases and assert outcomes.
- Parsing uses System.Text.Json; dates parsed as DateTimeOffset.

4) README.md (REQ-0011)
- Document:
  - How to build/test: dotnet build / dotnet test
  - How to run library in a sample snippet (no CLI required)
  - AI-assistance disclosure section (include what tools were used and for what)

QUALITY BARS / ACCEPTANCE CRITERIA (MUST MEET)
- All functional requirements REQ-0001..REQ-0011 implemented (where applicable to code) with tests covering REQ-0002..REQ-0010.
- Deterministic outputs: stable ordering, stable tie-breakers, permutation invariance for inputs.
- No UI/CLI/DB required to run core evaluation.
- Typed exceptions only; no `throw new Exception()`.
- `dotnet test` passes with repeat runs producing identical results.
- Code is idiomatic modern C# (nullable enabled, minimal allocations where obvious, no unnecessary async).

FILES TO CREATE (SUGGESTED)
- src/Retention.Domain/Entities/{Project,Environment,Release,Deployment}.cs
- src/Retention.Domain/Services/RetentionPolicyEvaluator.cs
- src/Retention.Application/EvaluateRetentionService.cs (or EvaluateRetentionUseCase.cs)
- src/Retention.Application/Models/{RetentionResult,KeptRelease,DecisionLogEntry,RetentionDiagnostics}.cs
- src/Retention.Application/Errors/{ValidationException,DomainException}.cs
- src/Retention.Application/Observability/RetentionTelemetry.cs (ActivitySource/Meter wrapper) [optional]
- tests/Retention.UnitTests/*Tests.cs
- tests/Retention.IntegrationTests/*Tests.cs
- README.md

IMPLEMENTATION ORDER
- Implement Domain evaluator + unit tests for ranking/ties first (SLICE-0001).
- Implement Application API + DTOs + validation + unit tests (SLICE-0002).
- Add integration + E2E tests (per docs/14_Test_Specification.md) and README (SLICE-0003).
- Only then add optional OTel instrumentation if desired (SLICE-0006).

SCM WORKFLOW (MANDATORY)
- Work in small, reviewable increments aligned to DSDS SLICE boundaries.
- After completing each SLICE, you MUST:
  1) run `dotnet test`
  2) confirm all tests pass
  3) `git status` shows only the intended changes for that SLICE
  4) create a git commit with a deterministic message format
- Do NOT begin the next SLICE until the previous SLICE is committed with all tests passing.
- If a SLICE requires multiple commits, use a suffix (`a`, `b`) but keep commits minimal.

COMMIT MESSAGE FORMAT (DETERMINISTIC)
Use exactly:
- `SLICE-0001: <short present-tense summary>`
- `SLICE-0002: <short present-tense summary>`
etc.

Each commit body MUST include:
- `Scope:` list of touched projects/files (high level)
- `Tests:` the exact command run (`dotnet test`) and result
- `Notes:` any TODOs/assumptions introduced (must reference docs/00_Assumptions_and_OpenQuestions.md if changed)

SLICE CHECKPOINTS (DEFAULT)
Unless DSDS defines otherwise, use these logical slices:
- SLICE-0001: Domain entities + retention evaluator + core ranking logic + unit tests for ranking/ties
- SLICE-0002: Application API + DTOs + validation + diagnostics + unit tests
- SLICE-0003: Integration tests + E2E (in-process) tests + sample inputs if required
- SLICE-0004: README with build/test instructions + AI disclosure
- SLICE-0005: Optional coordinated deletion boundary (only if required by DSDS; otherwise skip)
- SLICE-0006: Optional OTel instrumentation (only if DSDS includes NFR-0008 / ADR-0007 and you’ve already satisfied all functional requirements)

COMMANDS TO RUN AT EACH CHECKPOINT
At end of each SLICE:
- `dotnet build`
- `dotnet test`
- If either fails, fix within the same SLICE before committing.

DO NOT
- Do not commit failing tests.
- Do not mix unrelated refactors into a slice commit.
- Do not edit docs/** unless explicitly required by the DSDS and only in a dedicated “docs-only” commit.

NOW PROCEED
Do the work now: create the solution/projects, implement the code, and ensure tests pass.
Implement slice-by-slice and commit at each checkpoint following the rules above.

