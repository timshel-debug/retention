# ADR-0003: API client returns discriminated union (UI)

## Context
UI must distinguish ProblemDetails server errors from network failures. [Source: docs/inputs/requirements_source.md#UI-REQ-0010-—-Error-UX-ProblemDetails]

## Decision
API client wraps fetch and returns `ok | problem | network` rather than throwing.

## Consequences
Simplifies UI components and centralizes error parsing.
