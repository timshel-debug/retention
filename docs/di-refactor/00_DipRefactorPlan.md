# Dependency Inversion Principle (DIP) Refactor Plan

## Objective
Refactor the Retention codebase so that all service graph wiring occurs in composition roots (DI registration at startup), not inside application or domain service constructors.

## What counts as a "service"
Any class in `Retention.Application` or `Retention.Domain` that:
- Has dependencies on other services (via constructor parameters)
- Performs logic beyond pure data carrying
- Is registered in DI and resolved at runtime

Examples: `EvaluateRetentionService`, `RetentionEvaluationEngine`, `RetentionPolicyEvaluator`, `DefaultGroupRetentionEvaluator`, pipeline steps, validation rules, mappers, specification classes.

## What is exempt
- **Records / entities / DTOs**: `Project`, `Release`, `Deployment`, `Environment`, `RetentionResult`, `KeptRelease`, `DecisionLogEntry`, `ReleaseCandidate`, `GroupEntry`, `RankedCandidate`, etc.
- **Value objects**: `FilteredDeploymentsResult`, `ReferenceIndex`, `RetentionEvaluationInputs`, `ValidationContext`, `DeploymentValidityResult`
- **Static helpers with no state** (e.g., `ErrorCodes`, `DecisionReasonCodes`, `ReasonCodes`)

## The rule
> **No default constructors that construct collaborators; no `new XService()` in Application/Domain service types.**

Service classes MUST receive all collaborators via constructor injection. The only place `new` is used for services is the composition root.

## Composition roots
1. **Production**: `Retention.Api/Program.cs` → calls `builder.Services.AddRetentionApplication()`
2. **DI extensions**: `Retention.Application/DependencyInjection/ServiceCollectionExtensions.cs` (calls into `AddRetentionDomain()`)
3. **Test setup**: Tests may construct services directly with explicit dependencies, but MUST NOT rely on parameterless service constructors.

## Current offenders

| File | Issue |
|------|-------|
| `Retention.Application/EvaluateRetentionService.cs` | Default ctor `new`s `RetentionPolicyEvaluator`, `TelemetryGroupRetentionEvaluator`, `DefaultGroupRetentionEvaluator`. Also `new`s `RetentionEvaluationEngine` inside the 1-arg ctor. |
| `Retention.Application/Evaluation/RetentionEvaluationEngine.cs` | 1-arg ctor `new`s `ValidationRuleChainFactory.CreateDefaultChain()`, `ReferenceIndexBuilder`, `DefaultDeploymentValiditySpecification`, `DecisionLogAssembler`, `KeptReleaseMapper`, `DiagnosticsCalculator`, and all 7 pipeline steps. |
| `Retention.Application/Validation/ValidationRuleChainFactory.cs` | Static factory builds validation rule instances via `new`. |
| `Retention.Domain/Services/RetentionPolicyEvaluator.cs` | Parameterless ctor `new`s `DefaultGroupRetentionEvaluator`. |
| `Retention.Domain/Services/GroupRetentionEvaluator.cs` | Parameterless ctor `new`s `DefaultRankingStrategy` and `TopNSelectionStrategy`. |

## Slice plan
- **SLICE-DIP-000**: This document (no functional changes)
- **SLICE-DIP-001**: Introduce centralized DI registration extensions
- **SLICE-DIP-002**: API consumes abstractions (`IEvaluateRetentionService`)
- **SLICE-DIP-003**: Domain services remove parameterless constructors
- **SLICE-DIP-004**: Validation chain wired via DI
- **SLICE-DIP-005**: Evaluation engine receives DI-supplied steps
- **SLICE-DIP-006**: `EvaluateRetentionService` fully DI-driven
- **SLICE-DIP-007**: Benchmarks use same DI composition
- **SLICE-DIP-008**: Architecture guardrail tests
- **SLICE-DIP-009**: Cleanup dead wiring
