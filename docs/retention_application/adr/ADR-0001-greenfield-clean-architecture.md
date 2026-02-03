# ADR-0001 Greenfield Retention Module with Clean Architecture

## Context
Release Retention requires reusable, testable logic with tests and without UI/CLI/DB. [Source: Start Here - Instructions - Release Retention.md#The Task]

## Decision
Implement as a greenfield module using Clean Architecture:
- Domain policy isolated from infrastructure
- Application use case orchestrates validation and results

## Options Considered
1. Single project with mixed concerns
2. Clean Architecture (chosen)

## Consequences
- Clear boundaries and testability (NFR-0001, NFR-0006)
- Slightly more project structure overhead
