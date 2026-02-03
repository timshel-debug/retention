# Requirements

## Functional Requirements

REQ-0001 The system shall accept in-memory sets of **Projects**, **Environments**, **Releases**, and **Deployments** and evaluate retention using parameter `n` (releases to keep).  
Acceptance Criteria:
- Evaluation completes without external dependencies (no DB/UI/CLI). [Source: Start Here - Instructions - Release Retention.md:L25-L40]

REQ-0002 The system shall treat a release as “deployed” to an environment only if it has **one or more deployments** to that environment.  
Acceptance Criteria:
- Releases with zero deployments to a given environment are not eligible for retention selection in that environment. [Source: Start Here - Instructions - Release Retention.md:L27-L33]

REQ-0003 For each **project/environment** combination, the system shall keep the `n` releases that have been **most recently deployed** to that environment.  
Acceptance Criteria:
- For each project/environment, the output includes at most `n` kept releases for that project/environment. [Source: Start Here - Instructions - Release Retention.md:L27-L33]

REQ-0004 The system shall compute “most recently deployed” for a release using the **maximum deployment timestamp** (`max(DeployedAt)`) for that release within the project/environment combination.  
Acceptance Criteria:
- If a release has multiple deployments to the same environment, the latest `DeployedAt` is used for ranking. [Source: Start Here - Instructions - Release Retention.md:L27-L33]

REQ-0005 The system shall apply deterministic tie-breakers when ranking releases with equal latest deployment timestamps.  
Acceptance Criteria:
- Selection is stable using the tie-breakers in A-0006. [Source: Releases.json]

REQ-0006 The system shall evaluate retention across **all environments in the input set** in a single invocation.  
Acceptance Criteria:
- The “different environments” sample case returns kept releases for both environments when `n=1`. [Source: Start Here - Instructions - Release Retention.md:L75-L86]

REQ-0007 The system shall return the set of releases that should be kept.  
Acceptance Criteria:
- Output contains unique `(ProjectId, EnvironmentId, ReleaseId)` combinations and is deterministically ordered. [Source: Start Here - Instructions - Release Retention.md:L25-L40]

REQ-0008 The system shall produce a **decision log** for each kept release indicating why it was kept, including project id, environment id, `n`, rank, and latest deployment timestamp.  
Acceptance Criteria:
- Decision entries are structured and suitable for tests to assert “reason” fields. [Source: Start Here - Instructions - Release Retention.md:L25-L40]

REQ-0009 The system shall validate `n` and reject negative values.  
Acceptance Criteria:
- `n < 0` yields a validation failure with a stable error code. [Source: Start Here - Instructions - Release Retention.md:L25-L40]

REQ-0010 The system shall handle invalid references in inputs (e.g., deployment references missing environment) by excluding them from eligibility calculations and emitting a diagnostic decision entry.  
Acceptance Criteria:
- Such deployments do not cause evaluation to throw at the domain/application layers. [Source: Deployments.json]

REQ-0011 The solution repository shall include a `README.md` documenting AI-assistance usage (if any) per the exercise instructions.  
Acceptance Criteria:
- README answers the three AI-assistance questions when AI is used. [Source: Start Here - Instructions - Release Retention.md:L88-L94]

## Non-Functional Requirements

NFR-0001 The core retention logic shall be reusable and testable (pure functions / dependency-injected collaborators). [Source: Start Here - Instructions - Release Retention.md:L25-L40]

NFR-0002 The solution shall not require a UI, CLI, or database to run its core retention evaluation. [Source: Start Here - Instructions - Release Retention.md:L25-L40]

NFR-0003 The evaluation output shall be deterministic: stable ordering, no time-based randomness, and explicit tie-breakers. [Source: Start Here - Instructions - Release Retention.md:L25-L40]

NFR-0004 The solution shall scale to large inputs using single-pass grouping and bounded sorting per project/environment (complexity target: O(D + R log R) per project/environment). Performance targets TODO. [Source: Start Here - Instructions - Release Retention.md:L10-L22]

NFR-0005 The solution shall provide structured “why kept” information without leaking secrets/PII (ids/timestamps only by default). [Source: Start Here - Instructions - Release Retention.md:L25-L40]

NFR-0006 The design shall apply Clean Architecture and SOLID, isolating domain policy from infrastructure concerns. [Source: Start Here - Instructions - Release Retention.md:L25-L40]

NFR-0008 The solution should provide host-configurable observability via OpenTelemetry-friendly instrumentation (tracing, metrics) without coupling to a specific backend. (Addendum) [Source: docs/12_Observability_Addendum.md#Telemetry Model]


## Traceability

| ID | Components | Test Types | Ops Controls |
|---|---|---|---|
| REQ-0001 | Retention.Application, Retention.Engine | Unit, Contract | Input validation logs |
| REQ-0002 | Retention.Domain | Unit | Decision log counters |
| REQ-0003 | Retention.Domain | Unit, Property | Deterministic ordering checks |
| REQ-0004 | Retention.Domain | Unit | Decision log includes latest DeployedAt |
| REQ-0005 | Retention.Domain | Unit | Stable tie-breaker tests |
| REQ-0006 | Retention.Domain, Retention.Application | Unit | Environment coverage assertion |
| REQ-0007 | Retention.Application | Unit | Output ordering asserted |
| REQ-0008 | Retention.Application | Unit | Structured decisions validated |
| REQ-0009 | Retention.Application | Unit | Error code mapping |
| REQ-0010 | Retention.Application | Unit | Invalid reference handling tests |
| REQ-0011 | Repo docs | Review | README checklist gate |
| NFR-0001 | All | Unit | CI gate: tests required |
| NFR-0002 | Architecture | Review | No forbidden deps gate |
| NFR-0003 | Domain | Unit, Property | Determinism gate |
| NFR-0004 | Domain | Performance (optional) | Metrics: eval duration |
| NFR-0005 | Boundary | Unit | Log sanitization |
| NFR-0006 | Architecture | Review | Layering constraints |
