# Source Index

## Primary Inputs (Authoritative)

1. `Start Here - Instructions - Release Retention.md` — domain concepts, Release Retention rule, implementation constraints, sample test cases, and README AI-disclosure requirement. [Source: Start Here - Instructions - Release Retention.md:L25-L40]
2. `Projects.json` — sample project identifiers and names. [Source: Projects.json]
3. `Environments.json` — sample environment identifiers and names. [Source: Environments.json]
4. `Releases.json` — sample releases per project (id, version, created). [Source: Releases.json]
5. `Deployments.json` — sample deployments (release id, environment id, deployed time). [Source: Deployments.json]

## Non-Authoritative Artifacts (Not Inputs)

- `docs/inputs/Chat_Constraints.md` existed in the prior DSDS bundle but is **not** part of the original exercise requirements and must not constrain the implementation. It is removed from this reviewed DSDS. [Source: Start Here - Instructions - Release Retention.md:L25-L40]

## Identified Gaps in Source Inputs

- Tie-breakers for equal “most recently deployed” timestamps are not defined. A deterministic default is defined in this DSDS and isolated as an assumption (A-0006). [Source: Start Here - Instructions - Release Retention.md:L27-L33]
- Sample data includes a deployment referencing an environment id not present in `Environments.json` (`Environment-3`). Behavior is unspecified; this DSDS defines deterministic “exclude + diagnose” behavior (A-0005 / ADR-0005). [Source: Deployments.json]

## Additional Inputs (Addendum)

- `docs/inputs/Addendum_Request_Observability_and_Deletion.md` (observability + coordinated deletion addendum request).
