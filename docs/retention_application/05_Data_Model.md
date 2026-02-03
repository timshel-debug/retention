# Data Model

## Entity Model

```mermaid
erDiagram
  PROJECT ||--o{ RELEASE : "owns"
  RELEASE ||--o{ DEPLOYMENT : "instance of"
  ENVIRONMENT ||--o{ DEPLOYMENT : "receives"
  RELEASE ||--o{ KEPT_RELEASE : "evaluated to"
  
  PROJECT {
    string Id PK
    string Name
  }
  
  ENVIRONMENT {
    string Id PK
    string Name
  }
  
  RELEASE {
    string Id PK
    string ProjectId FK
    string Version
    datetime Created
  }
  
  DEPLOYMENT {
    string Id PK
    string ReleaseId FK
    string EnvironmentId FK
    datetime DeployedAt
  }
  
  KEPT_RELEASE {
    string ReleaseId
    string ProjectId
    string EnvironmentId
    DateTimeOffset LatestDeployedAt
    int Rank
    string ReasonCode
  }
```

## Logical Entities

### Project
- `Id` (string)
- `Name` (string) [Source: Projects.json]

### Environment
- `Id` (string)
- `Name` (string) [Source: Environments.json]

### Release
- `Id` (string)
- `ProjectId` (string)
- `Version` (string | null)
- `Created` (datetime string) [Source: Releases.json]

### Deployment
- `Id` (string)
- `ReleaseId` (string)
- `EnvironmentId` (string)
- `DeployedAt` (datetime string) [Source: Deployments.json]

## Relationships
- Project 1..* Release
- Release 0..* Deployment
- Environment 0..* Deployment [Source: Start Here - Instructions - Release Retention.md:L25-L40]

## Retention Outputs (DTOs)

### KeptRelease
- `ReleaseId`
- `ProjectId`
- `EnvironmentId`
- `Version` (copied from Release)
- `Created` (copied from Release)
- `LatestDeployedAt`
- `Rank`
- `ReasonCode` (stable enum/string)

### DecisionLogEntry
- `ProjectId`
- `EnvironmentId`
- `ReleaseId`
- `n`
- `Rank`
- `LatestDeployedAt`
- `ReasonText`

## Retention and PII
- PII classification is TODO (inputs are ids/names only in samples).  
- Log entries must avoid secrets and should not dump full payloads by default. [Source: Start Here - Instructions - Release Retention.md:L25-L40]

## Migrations / Rollout
- No database is introduced by this solution (NFR-0002). [Source: Start Here - Instructions - Release Retention.md:L25-L40]
