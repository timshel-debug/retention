# Test Strategy and Gates

## Test Pyramid

- Unit Tests (majority):
  - RetentionPolicyEvaluator ranking, eligibility, tie-breakers, multi-environment evaluation.
  - Validation rules and error mapping.
- Contract Tests:
  - Public `EvaluateRetention` contract (input/output DTO shape and deterministic ordering).

## Required Test Cases (Minimum)

- `n = 0` keeps nothing (REQ-0009).
- Single deployed release keeps 1 (sample test case). [Source: Start Here - Instructions - Release Retention.md:L25-L40]
- Multiple releases same env keep top `n` by deployed time (REQ-0003). [Source: Start Here - Instructions - Release Retention.md:L25-L40]
- Different environments are evaluated in a single invocation; keep `n` per environment (REQ-0006). [Source: Start Here - Instructions - Release Retention.md:L75-L86]
- Release with multiple deployments uses max DeployedAt (REQ-0004).
- Equal DeployedAt tie-breakers are deterministic (REQ-0005, Q-0001).
- Invalid environment reference in deployment is excluded and produces diagnostic entry (REQ-0010). [Source: Deployments.json]

## Gates (Definition of Done)
- All unit tests pass.
- Determinism gate: re-run evaluation twice and assert identical outputs (NFR-0003).
- No forbidden dependencies: no UI/CLI/DB in core projects (NFR-0002).
- README checklist gate: AI-assistance disclosure included when AI is used (REQ-0011).
