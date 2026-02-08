# Pattern 03 — Specification (Deployment Validity + Reasons)

## Intent
Extract deployment reference integrity rules into a specification that produces (a) a boolean validity result and (b) structured reasons used for diagnostic decision log entries.

## Design
Introduce:
- `Retention.Application.Specifications.IDeploymentValiditySpecification`
- `Retention.Application.Specifications.DeploymentValidityResult`
- `Retention.Application.Specifications.DefaultDeploymentValiditySpecification`

### Specification contract
- SPEC-REQ-0001: The spec MUST expose:
  - `DeploymentValidityResult Evaluate(Deployment deployment, ReferenceIndex index, IReadOnlyDictionary<string, Release> releasesById)`
- `DeploymentValidityResult` MUST include:
  - `bool IsValid`
  - `IReadOnlyList<string> Reasons` (human-readable fragments, deterministic order)

### Default validity rules (match current)
- Release exists for `deployment.ReleaseId`, otherwise add reason `release '{id}' not found`
- If release exists, release.ProjectId must exist in projects, otherwise add `project '{id}' not found`
- Environment exists for `deployment.EnvironmentId`, otherwise add `environment '{id}' not found`

### Ordering of reasons
- SPEC-NFR-0001: Reasons MUST be appended in the same order as current implementation to preserve reason text.

## Requirements
- SPEC-REQ-0002: Spec evaluation MUST be side-effect-free and deterministic.
- SPEC-REQ-0003: Spec MUST support mapping invalid deployments to diagnostics entries without additional reference checks elsewhere.

## Acceptance criteria
- Given identical inputs, invalid deployments produce identical `ReasonText` fragments (string-join semantics preserved).

## Suggested file layout
- `src/Retention.Application/Specifications/IDeploymentValiditySpecification.cs`
- `src/Retention.Application/Specifications/DefaultDeploymentValiditySpecification.cs`
- `src/Retention.Application/Specifications/DeploymentValidityResult.cs`
