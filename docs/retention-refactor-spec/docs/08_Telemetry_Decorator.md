# Pattern 08 — Decorator (Telemetry) replacing callback-based spans

## Intent
Move telemetry concerns out of domain logic call signatures (remove `onRankGroup` callback) and replace with an instrumented decorator that wraps group evaluation with spans of meaningful duration.

## Design
Changes:
- Modify domain `IRetentionPolicyEvaluator.Evaluate(...)` to REMOVE the `onRankGroup` callback parameter.
- Provide `TelemetryRetentionPolicyEvaluator : IRetentionPolicyEvaluator` in application or domain (prefer application/infra) that:
  - Starts `Activity` spans per group *around actual group evaluation work*.
  - Records kept counts and duration.

To enable accurate timing:
- Domain evaluator MUST expose group evaluation execution in a way the decorator can time:
  - Option A: Evaluator internally iterates groups via an injected `IGroupRetentionEvaluator` and decorator wraps that.
  - Option B: Evaluator returns a grouped intermediate model, but this is less clean.

Telemetry APIs:
- Continue using `RetentionTelemetry.StartRankActivity(...)` and `RetentionTelemetry.RecordRankComplete(...)`.

## Requirements
- TEL-REQ-0001: Business behavior MUST be unchanged when telemetry is enabled/disabled.
- TEL-REQ-0002: The public `EvaluateRetentionService` MUST continue to record outer evaluation spans and errors as today.
- TEL-REQ-0003: Per-group spans MUST cover ranking+selection runtime (non-zero in non-trivial cases).

## Acceptance criteria
- Unit tests can run with telemetry disabled by default.
- A dedicated integration test can assert that group span creation occurs (optional, if telemetry abstractions allow inspection).
