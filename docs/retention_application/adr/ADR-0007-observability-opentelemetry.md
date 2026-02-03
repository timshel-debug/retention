# ADR-0007 Observability via OpenTelemetry Instrumentation

## Context
The coding exercise requires logging why a release is kept, but does not prescribe production-grade telemetry. [Source: Octopus_Deploy_Code_Puzzle-Release_Retention/Start Here - Instructions - Release Retention.md:L25-L40]
The addendum request asks for an explicit observability design and to lean into OpenTelemetry and related instrumentation. [Source: docs/inputs/Addendum_Request_Observability_and_Deletion.md#Request]

## Decision
Instrument the solution using OpenTelemetry-friendly primitives, without coupling to any specific backend:
- Tracing: emit spans around retention evaluation and per project/environment ranking operations.
- Metrics: emit counters/histograms for evaluation volume, latency, and result sizes.
- Logs: keep the required “why kept” decision log as a structured event stream; optionally mirror key fields to host logs.
Expose instrumentation via `ActivitySource` and `Meter` inside the library; exporter configuration belongs to the host.

## Options Considered
1. No telemetry beyond decision log (rejected: insufficient operational visibility for productionization).
2. Custom metrics/logging API (rejected: reinvents ecosystem standards).
3. OpenTelemetry primitives in library, exporter in host (chosen).

## Consequences
- Library remains reusable and testable; telemetry can be disabled in tests.
- Hosts (CLI/service) can choose exporters/collectors without code changes in the library.
- Additional instrumentation work is optional for the coding exercise; treated as addendum. [Source: docs/12_Observability_Addendum.md#Scope]
