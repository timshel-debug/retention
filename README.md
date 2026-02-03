# Release Retention Engine

A .NET library that evaluates which releases to keep based on deployment history. Designed for embedding in DevOps Deploy to reduce storage usage and improve performance by enabling deletion of older, inactive releases.

## Quick Start

```csharp
using Retention.Application;
using Retention.Domain.Entities;

// Create input data
var projects = new[] { new Project("Project-1", "My App") };
var environments = new[] { new Environment("Env-1", "Production") };
var releases = new[] { new Release("Release-1", "Project-1", "1.0.0", new DateTimeOffset(2000, 1, 1, 8, 0, 0, TimeSpan.Zero)) };
var deployments = new[] { new Deployment("Deploy-1", "Release-1", "Env-1", new DateTimeOffset(2000, 1, 1, 10, 0, 0, TimeSpan.Zero)) };

// Evaluate retention
var service = new EvaluateRetentionService();
var result = service.EvaluateRetention(
    projects, 
    environments, 
    releases, 
    deployments,
    releasesToKeep: 2);

// Access results
foreach (var kept in result.KeptReleases)
{
    Console.WriteLine($"Keep {kept.ReleaseId} for {kept.EnvironmentId} (rank {kept.Rank})");
}

// Check decision log
foreach (var decision in result.Decisions)
{
    Console.WriteLine($"{decision.ReasonCode}: {decision.ReasonText}");
}
```

## Build & Test

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal
```

## Project Structure

```
src/
├── Retention.Domain/           # Pure business rules (no dependencies)
│   ├── Entities/               # Project, Environment, Release, Deployment
│   ├── Models/                 # ReleaseCandidate, ReasonCodes
│   └── Services/               # RetentionPolicyEvaluator
│
└── Retention.Application/      # Application orchestration
    ├── Errors/                 # ValidationException, DomainException
    ├── Models/                 # DTOs (RetentionResult, KeptRelease, DecisionLogEntry)
    └── EvaluateRetentionService.cs

tests/
├── Retention.UnitTests/        # Domain and application unit tests
└── Retention.IntegrationTests/ # Contract tests and E2E tests
```

## Architecture

This solution follows **Clean Architecture** principles:

- **Domain Layer**: Contains pure business logic with no external dependencies. The `RetentionPolicyEvaluator` implements the core retention algorithm.
- **Application Layer**: Orchestrates validation, handles invalid references, and converts domain results to DTOs.

Key design decisions are documented in `docs/adr/`.

## Business Rules

For each **project/environment** combination, keep `n` releases that have been **most recently deployed**:

1. **Eligibility**: A release must have at least one deployment to an environment to be eligible for that environment
2. **Ranking**: Releases ranked by `max(DeployedAt)` for deployments to that environment
3. **Tie-breakers** (when deployment times are equal):
   - Release.Created desc (more recent wins)
   - Release.Id asc (alphabetical)

### Invalid Reference Handling

Deployments referencing missing projects, environments, or releases are:
- Excluded from eligibility calculations
- Recorded in the decision log with `diagnostic.invalid_reference` reason code

## API Reference

### `EvaluateRetentionService.EvaluateRetention`

```csharp
RetentionResult EvaluateRetention(
    IReadOnlyList<Project>? projects,
    IReadOnlyList<Environment>? environments,
    IReadOnlyList<Release>? releases,
    IReadOnlyList<Deployment>? deployments,
    int releasesToKeep,
    string? correlationId = null)
```

**Parameters:**
- `releasesToKeep`: Number of releases to keep per project/environment (n >= 0)
- `correlationId`: Optional identifier for tracing (not auto-generated)

**Returns:** `RetentionResult` containing:
- `KeptReleases`: Deterministically ordered list of releases to keep
- `Decisions`: Decision log entries (kept + diagnostic)
- `Diagnostics`: Counts (groups evaluated, invalid deployments, total kept)

**Throws:**
- `ValidationException` (code: `validation.n_negative`) if `releasesToKeep < 0`
- `ValidationException` (code: `validation.null_element`) if any collection contains null

## Sample Data

The `Octopus_Deploy_Code_Puzzle-Release_Retention/` directory contains sample JSON files demonstrating the expected input format:
- `Projects.json`
- `Environments.json`
- `Releases.json`
- `Deployments.json`

## AI Assistance Disclosure

This solution was developed entirely with AI assistance, following explicit architectural and implementation constraints:

**What AI assistant(s) did you use?**
- **ChatGPT** for solution design (superior reasoning for architecture and specification)
- **Claude Opus** for implementation (superior coding capability)

**What did you ask AI to assist with and why?**
- **Solution Design**: Used ChatGPT with the attached `solution-design-prompt.md` to generate comprehensive architecture decision records (ADRs), requirements specifications, and design documentation. This established the authoritative DSDS (Design, Security & Specification) that all implementation followed.
- **Full Implementation**: Used Claude Opus with the attached `instruction-prompt.md` to implement the complete codebase including:
  - Domain layer (retention algorithm, ranking, tie-breakers, entities)
  - Application layer (validation, DTOs, error handling, diagnostics)
  - API layer (controllers, middleware, RFC7807 error handling, observability instrumentation)
  - React UI (components, utilities, deterministic sorting, client-side validation)
  - Test suites (unit tests covering ranking/tie-breakers, integration tests for contracts, E2E tests)
  - Documentation (README, inline comments, architecture guides)
- **Code Review**: Used Claude Opus with the attached `code-review-prompt.md` to perform comprehensive code review across all layers, identifying 8 defects which were then systematically fixed.
- **Infrastructure**: Generated PowerShell orchestration scripts, test data, and deployment configuration.

**How useful did you find the AI output?**
- **Extremely useful** - The AI output was highly productive and required minimal intervention due to the explicit constraints provided in the prompts.
- **Critical factor for success**: The quality of AI output was directly proportional to the explicitness and determinism of the instructions. Vague or open-ended prompts resulted in off-track solutions; highly constrained prompts with specific requirements, DSDS references, and acceptance criteria produced publication-ready code.
- **Intervention strategy**: User reviewed and suggested improvements at each step:
  - At each SLICE checkpoint (code unit), validated that `dotnet test` passed and output matched DSDS requirements
  - Provided specific feedback to refine output (e.g., "add CORS configuration," "fix DIP violation," "implement FND-0006 observability")
  - Made tactical decisions about scope (e.g., defer optional features, prioritize determinism gates, fix security vulnerabilities)
- **Extra steps taken**:
  - All generated code was validated against the DSDS (docs/) for compliance
  - Test results verified with `dotnet test` and `npm test` at each commit
  - Manual testing of API/UI integration to ensure real-world functionality
  - Code review findings systematically remediated with targeted fixes
  - Edge cases and determinism requirements validated through shuffle tests and repeated runs
- **Key insight**: LLMs excel at following explicit, deterministic instructions but struggle with ambiguous or open-ended problems. Success required upfront investment in detailed specifications and constraints that left no room for interpretation.

## Requirements Coverage

| Requirement | Implementation |
|-------------|----------------|
| REQ-0002 | Eligibility requires ≥1 deployment |
| REQ-0003 | Keep top n by most recent deployment |
| REQ-0004 | Use max(DeployedAt) for ranking |
| REQ-0005 | Deterministic tie-breakers (ADR-0003) |
| REQ-0006 | Multi-environment evaluation |
| REQ-0007 | Return kept releases |
| REQ-0008 | Decision log with reasons |
| REQ-0009 | Validate n ≥ 0 |
| REQ-0010 | Invalid reference handling |
| NFR-0003 | Deterministic outputs |

## License

This is a coding exercise submission.
