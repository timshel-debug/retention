# Assumptions and Open Questions

## Assumptions (used to proceed)
- Test framework is available (prefer xUnit). If no test project exists, create `Retention.Tests` with xUnit and target the same .NET version as the solution.
- Telemetry uses `RetentionTelemetry` and `System.Diagnostics.Activity` and must remain optional and side-effect-free from a business logic perspective.
- `DecisionLogEntry.DecisionType == "kept"` is the canonical signal for “kept” entries (as implied by sorting). If this is derived property instead, sorting should use the derived behavior consistently.

## Open questions (record only; do not block)
- Whether decision text strings must be byte-for-byte identical or only semantically equivalent. This spec assumes “prefer identical”.
- Whether DI container is used by the host application. This spec assumes library can remain usable without DI via default constructors, but DI-friendly constructors will be added.
