# Implementation Prompt — Retention Refactor (Apply All Patterns)

## Objective
Refactor the existing retention evaluation solution to implement all patterns specified in `docs/01..10_*.md` while preserving behavior and public API.

## Hard constraints
- Preserve `IEvaluateRetentionService.EvaluateRetention(...)` signature and semantics.
- Preserve deterministic ordering and existing error codes/messages.
- No business rule changes: keep Top N by latest deployment with tie-breakers.
- No new side effects (pure engine).

## Target structure (minimum)
### Application layer
- `Retention.Application/Evaluation/*` (engine, context, steps)
- `Retention.Application/Validation/*` (rule chain)
- `Retention.Application/Specifications/*` (deployment validity spec)
- `Retention.Application/Indexing/*` (reference index + builder)
- `Retention.Application/Mapping/*` (mappers + assemblers)

### Domain layer
- `Retention.Domain/Services/*` refactor evaluator to:
  - remove callback param
  - use `IRetentionRankingStrategy` + `IRetentionSelectionStrategy`
  - use Template Method / group evaluator abstraction to enable telemetry decorator timing

### Telemetry
- Provide `TelemetryRetentionPolicyEvaluator : IRetentionPolicyEvaluator` wrapper (location: Application or Infrastructure, but referenced by app service composition)

## Required steps (do in order)
1. **Golden tests first**
   - Add tests that capture current outputs for representative fixtures:
     - No data
     - releasesToKeep = 0
     - Single project/env with multiple deployments and ties
     - Multiple groups
     - Invalid references (missing release, missing project, missing env)
   - Snapshot compare: `KeptReleases`, `Decisions` (including text), `Diagnostics`.

2. **Introduce ReferenceIndex + builder** (Pattern 09)
3. **Introduce deployment validity specification + filter result** (Patterns 03, 04)
4. **Introduce mapper/assembler components** (Pattern 07)
5. **Refactor domain evaluator to Strategy + Template Method** (Patterns 05, 06)
   - Keep default behavior identical.
   - Remove `onRankGroup` callback from the interface and implementation.
6. **Add telemetry decorator around group evaluation** (Pattern 08)
7. **Build evaluation pipeline + pure engine** (Patterns 01, 10)
   - Shell (`EvaluateRetentionService`) manages Activity/Stopwatch and calls engine.
   - Engine composes pipeline steps and returns `RetentionResult`.

## Acceptance criteria checklist
- All golden tests pass and match pre-refactor snapshots.
- Public API unchanged (`IEvaluateRetentionService` method signature).
- `ValidationException` error codes match on negative `releasesToKeep`, null elements, duplicate IDs.
- Invalid deployments excluded and logged identically.
- Deterministic ordering unchanged.
- Per-group telemetry spans have non-zero durations for non-trivial datasets (if testable; otherwise verify by manual run).

## Notes
- Keep refactor in small commits with compile+tests green at each commit.
- Do not “simplify” decision text; keep as-is unless tests prove safe.
