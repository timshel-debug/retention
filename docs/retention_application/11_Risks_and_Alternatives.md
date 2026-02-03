# Risks and Alternatives

## Risk Matrix

| Risk ID | Risk | Likelihood | Impact | Mitigation |
|---|---|---:|---:|---|
| R-0001 | Ambiguity in tie-breakers for equal timestamps | Medium | Medium | Fix tie-breakers (A-0006) + tests; revisit if product defines different semantics (Q-0001). |
| R-0002 | Invalid references in input data reduce retained set | Medium | Medium | Exclude invalid deployments with diagnostics (REQ-0010) and add an “invalid count” assertion in tests. |
| R-0003 | Misinterpreting environment scoping causes incorrect retention results | Medium | High | Explicitly test multi-environment sample case (REQ-0006). [Source: Start Here - Instructions - Release Retention.md:L75-L86] |
| R-0004 | Performance degradation with very large release histories | Medium | Medium | Single-pass grouping and bounded sorting; add performance tests when targets are defined (NFR-0004). |
| R-0005 | Over-logging increases noise | Low | Medium | Return decision log; caller controls emission. |

## Alternatives

### ALT-0001 Implement as pure library only
- Pros: aligns with “no UI/CLI/DB”; easiest to embed and test.
- Cons: any scheduling/orchestration is pushed to embedding system. [Source: Start Here - Instructions - Release Retention.md:L25-L40]

### ALT-0002 Add an optional process host (worker)
- Pros: clear boundary for structured logging.
- Cons: not required by the exercise; additional moving parts.

### ALT-0003 Coordinated deletion of artifacts/logs
- Pros: aligns with storage motivation.
- Cons: **not required by the coding exercise**; store contracts are not provided. Treat as discussion topic only. [Source: Start Here - Instructions - Release Retention.md:L10-L22]

## Cost Notes
- No pricing or infrastructure cost inputs provided; omitted by design.
