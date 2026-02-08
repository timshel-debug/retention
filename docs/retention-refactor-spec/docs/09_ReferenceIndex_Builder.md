# Pattern 09 — Builder (ReferenceIndex)

## Intent
Centralize construction of lookup dictionaries and guarantee consistent comparers and semantics.

## Design
Introduce:
- `Retention.Application.Indexing.ReferenceIndex`
  - `IReadOnlyDictionary<string, Project> ProjectsById`
  - `IReadOnlyDictionary<string, Environment> EnvironmentsById`
  - `IReadOnlyDictionary<string, Release> ReleasesById`
- `IReferenceIndexBuilder`
  - `ReferenceIndex Build(IReadOnlyList<Project> projects, IReadOnlyList<Environment> environments, IReadOnlyList<Release> releases)`

Default builder:
- Uses `ToDictionary(x => x.Id, StringComparer.Ordinal)` where applicable (if API available) or ensures ordinal semantics at lookup points.

## Requirements
- IDX-REQ-0001: Index build MUST assume duplicates have been validated already; it MUST not re-validate duplicates.
- IDX-REQ-0002: Index build MUST throw only on programmer error (e.g., null args), not on user input (validation layer handles user input).

## Acceptance criteria
- Index builder produces dictionaries equivalent to previous `ToDictionary(p => p.Id)` behavior (ordinal key comparisons maintained across codepaths).
