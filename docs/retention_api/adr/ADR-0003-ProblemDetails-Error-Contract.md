# ADR-0003: RFC7807 ProblemDetails unified error contract (API)

## Context
API requires consistent error responses with stable `error_code` and `trace_id` and no stack traces. [Source: docs/inputs/requirements_source.md#API-REQ-0007-—-Error-Handling-via-ProblemDetails]

## Decision
Implement a single global exception-handling middleware that maps typed failures to ProblemDetails with required extensions.

## Consequences
Boundary-only error shaping; simpler controllers and consistent behaviour.
