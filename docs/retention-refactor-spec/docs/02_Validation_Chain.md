# Pattern 02 — Chain of Responsibility (Validation + Reference Checks)

## Intent
Convert ad-hoc validation calls into a composable rule chain with stable error codes/messages.

## Design
Introduce:
- `Retention.Application.Validation.IValidationRule<TContext>`
- `Retention.Application.Validation.ValidationRuleChain<TContext>` (or just `IReadOnlyList<IValidationRule<TContext>>` executed in order)
- Concrete rules:

### Rules (minimum)
- `NonNegativeReleasesToKeepRule` (maps to `ErrorCodes.NNegative`)
- `NoNullElementsRule<Project>` (maps to `ErrorCodes.NullElement`, paramName `projects`)
- `NoNullElementsRule<Environment>` (paramName `environments`)
- `NoNullElementsRule<Release>` (paramName `releases`)
- `NoNullElementsRule<Deployment>` (paramName `deployments`)
- `NoDuplicateIdsRule<Project>` (maps to `ErrorCodes.DuplicateProjectId`)
- `NoDuplicateIdsRule<Environment>` (maps to `ErrorCodes.DuplicateEnvironmentId`)
- `NoDuplicateIdsRule<Release>` (maps to `ErrorCodes.DuplicateReleaseId`)

Reference checks are handled in the Specification + Filter step (Pattern 03), not in validation, to keep “invalid reference” from being a throw.

## Requirements
- VAL-REQ-0001: Validation MUST run before any lookups are built.
- VAL-REQ-0002: Validation errors MUST throw `ValidationException` with identical error codes and equivalent messages.
- VAL-REQ-0003: Rule execution order MUST be deterministic and explicitly declared.

## Acceptance criteria
- Each former validation failure case is reproduced by an equivalent rule test.
- No validation logic remains embedded in `EvaluateRetentionService` except calling the chain.

## Suggested file layout
- `src/Retention.Application/Validation/IValidationRule.cs`
- `src/Retention.Application/Validation/Rules/*.cs`
