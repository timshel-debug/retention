/**
 * Core dataset types for Release Retention Console.
 * Must match API contracts.
 */

export interface Project {
  id: string;
  name: string;
}

export interface Environment {
  id: string;
  name: string;
}

export interface Release {
  id: string;
  projectId: string;
  version?: string | null;
  created: string; // ISO 8601
}

export interface Deployment {
  id: string;
  releaseId: string;
  environmentId: string;
  deployedAt: string; // ISO 8601
}

export interface Dataset {
  projects: Project[];
  environments: Environment[];
  releases: Release[];
  deployments: Deployment[];
}

/**
 * Validation response types.
 */
export interface ValidationMessage {
  code: string;
  message: string;
  path: string | null;
}

export interface ValidationSummary {
  projectCount: number;
  environmentCount: number;
  releaseCount: number;
  deploymentCount: number;
  errorCount: number;
  warningCount: number;
}

export interface ValidationResponse {
  isValid: boolean;
  errors: ValidationMessage[];
  warnings: ValidationMessage[];
  summary: ValidationSummary;
}

/**
 * Evaluation response types.
 */
export interface KeptRelease {
  releaseId: string;
  projectId: string;
  environmentId: string;
  version: string | null;
  created: string;
  latestDeployedAt: string;
  rank: number;
  reasonCode: string;
}

export interface Decision {
  projectId: string;
  environmentId: string;
  releaseId: string;
  n: number;
  rank: number;
  latestDeployedAt: string | null;
  reasonCode: string;
  reasonText: string;
}

export interface Diagnostics {
  groupsEvaluated: number;
  invalidDeploymentsExcluded: number;
  totalKeptReleases: number;
}

export interface EvaluationResponse {
  keptReleases: KeptRelease[];
  decisions: Decision[];
  diagnostics: Diagnostics;
  correlationId?: string | null;
}

/**
 * RFC 7807 ProblemDetails with extensions.
 */
export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  // Extensions
  error_code?: string;
  trace_id?: string;
  correlation_id?: string;
  validation_errors?: ValidationMessage[];
}

/**
 * API result discriminated union.
 */
export type ApiResult<T> =
  | { kind: 'ok'; data: T }
  | { kind: 'problem'; problem: ProblemDetails }
  | { kind: 'network'; error: Error };

/**
 * Request types.
 */
export interface EvaluateRetentionRequest {
  dataset: Dataset;
  releasesToKeep: number;
  correlationId?: string;
}

export interface ValidateDatasetRequest {
  dataset: Dataset;
  correlationId?: string;
}
