# Assumptions and Open Questions

## Assumptions

A-0001 MODE is **B (Greenfield)**: implement Release Retention as a standalone module suitable for embedding in DevOps Deploy. [Source: Start Here - Instructions - Release Retention.md:L25-L40]

A-0002 “Most recently been deployed” is based on the **maximum `DeployedAt`** timestamp for the release within a given project/environment combination. [Source: Start Here - Instructions - Release Retention.md:L27-L33]

A-0003 “Keep `n` releases” applies **per project/environment**, and the final kept set is the **union** across all project/environment combinations evaluated in a single invocation. [Source: Start Here - Instructions - Release Retention.md:L27-L33]

A-0004 Releases with **zero deployments** to a given project/environment are **not eligible** to be kept for that project/environment. [Source: Start Here - Instructions - Release Retention.md:L27-L33]

A-0005 **Invalid references:** deployments that reference missing releases/environments/projects are excluded from eligibility calculations and produce a diagnostic decision entry. Impact: data quality issues reduce retained set and require upstream remediation. [Source: Deployments.json]

A-0006 Deterministic tie-breakers when two releases have the same “latest deployment” timestamp within a project/environment:
1) later `Release.Created` wins (descending),
2) then `Release.Id` wins (ascending, ordinal). [Source: Releases.json]

A-0007 `n` (releases to keep) is an integer parameter with constraint `n >= 0`. If `n == 0`, nothing is kept. [Source: Start Here - Instructions - Release Retention.md:L25-L40]

## Open Questions

Q-0001 Tie-breakers for equal latest deployment timestamps are not defined by the requirements beyond “most recently deployed”.
- Reasoning: required for deterministic outputs and testability under equal timestamps.  
- Default used: A-0006.  
- Impact if changed: selection order changes for edge cases; update unit tests and downstream expectations. [Source: Start Here - Instructions - Release Retention.md:L27-L33]

Q-0002 Invalid reference handling is not defined (e.g., deployment references unknown environment).
- Reasoning: supplied sample data contains invalid references; implementation must be deterministic and robust.
- Default used: A-0005 / ADR-0005.  
- Impact if changed: failing fast can break retention evaluation for otherwise valid data; update tests and error strategy. [Source: Deployments.json]

Q-0003 “Log why kept” sink is not defined (return value vs side-effect logging).
- Reasoning: exercise requires logging but also no UI/CLI/DB; implementation should remain testable.
- Default design: return a `DecisionLog` as part of the result; caller/host emits logs using returned entries.  
- Impact: if strict side-effect logging is required, add an optional logger callback without affecting domain purity. [Source: Start Here - Instructions - Release Retention.md:L25-L40]
