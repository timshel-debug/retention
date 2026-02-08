# Pattern 10 — Functional Core + Imperative Shell

## Intent
Create a pure, deterministic engine that performs evaluation without telemetry/time concerns. Wrap it in an outer shell that handles Activity/Stopwatch and error recording.

## Design
Introduce:
- `Retention.Application.Evaluation.RetentionEvaluationEngine`
  - `RetentionResult Evaluate(RetentionEvaluationInputs inputs)` (pure)
- `RetentionEvaluationInputs` wraps all input parameters (including correlationId).
- `EvaluateRetentionService` becomes the shell:
  - Normalizes null lists (or delegates normalization to a step)
  - Starts outer activity/stopwatch
  - Calls engine
  - Records complete/error telemetry
  - Returns result

## Requirements
- CORE-REQ-0001: Engine MUST be side-effect-free and deterministic (no Activity usage, no Stopwatch).
- CORE-REQ-0002: Shell MUST preserve existing telemetry behavior and exception recording.
- CORE-REQ-0003: Public API of `IEvaluateRetentionService` remains unchanged.

## Acceptance criteria
- Engine can be unit-tested without any telemetry setup.
- Shell tests confirm telemetry hooks are invoked on success/failure.
