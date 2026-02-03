/**
 * Client-side dataset validation.
 * Performs structural checks and reference validation without server roundtrip.
 * [Source: docs/inputs/requirements_source.md#UI-REQ-0003-—-Client-Side-Validation]
 */

import type { Dataset, ValidationMessage, ValidationResponse } from '../../shared/types';
import { buildJsonPath } from '../../shared/utils/jsonPath';
import { stableSort, Comparators } from '../../shared/utils/sorting';

/**
 * Error codes for client-side validation.
 */
const ErrorCodes = {
  MISSING_REQUIRED: 'validation.missing_required_field',
  INVALID_TYPE: 'validation.invalid_type',
  DUPLICATE_ID: 'validation.duplicate_id',
  INVALID_REFERENCE: 'validation.invalid_reference',
} as const;

/**
 * Validates that a required string field is present and non-empty.
 */
function validateRequiredString(
  value: unknown,
  collection: string,
  index: number,
  property: string,
  errors: ValidationMessage[]
): void {
  if (value === null || value === undefined || value === '') {
    errors.push({
      code: ErrorCodes.MISSING_REQUIRED,
      message: `${property} is required`,
      path: buildJsonPath(collection, index, property),
    });
  } else if (typeof value !== 'string') {
    errors.push({
      code: ErrorCodes.INVALID_TYPE,
      message: `${property} must be a string`,
      path: buildJsonPath(collection, index, property),
    });
  }
}

/**
 * Validates that a date string is valid ISO 8601.
 */
function validateDateString(
  value: unknown,
  collection: string,
  index: number,
  property: string,
  errors: ValidationMessage[]
): void {
  if (value === null || value === undefined || value === '') {
    errors.push({
      code: ErrorCodes.MISSING_REQUIRED,
      message: `${property} is required`,
      path: buildJsonPath(collection, index, property),
    });
    return;
  }

  if (typeof value !== 'string') {
    errors.push({
      code: ErrorCodes.INVALID_TYPE,
      message: `${property} must be a string`,
      path: buildJsonPath(collection, index, property),
    });
    return;
  }

  const date = new Date(value);
  if (isNaN(date.getTime())) {
    errors.push({
      code: ErrorCodes.INVALID_TYPE,
      message: `${property} must be a valid ISO 8601 date string`,
      path: buildJsonPath(collection, index, property),
    });
  }
}

/**
 * Validates uniqueness of IDs within a collection.
 */
function validateUniqueIds<T extends { id: string }>(
  items: T[],
  collection: string,
  errors: ValidationMessage[]
): void {
  const seenIds = new Map<string, number>();

  items.forEach((item, index) => {
    if (!item.id) return; // Skip if ID is missing (caught by required validation)

    const previousIndex = seenIds.get(item.id);
    if (previousIndex !== undefined) {
      errors.push({
        code: ErrorCodes.DUPLICATE_ID,
        message: `Duplicate ID '${item.id}' (first seen at index ${previousIndex})`,
        path: buildJsonPath(collection, index, 'id'),
      });
    } else {
      seenIds.set(item.id, index);
    }
  });
}

/**
 * Validates projects.
 */
function validateProjects(dataset: Dataset, errors: ValidationMessage[]): void {
  dataset.projects.forEach((project, index) => {
    validateRequiredString(project.id, 'projects', index, 'id', errors);
    validateRequiredString(project.name, 'projects', index, 'name', errors);
  });
  validateUniqueIds(dataset.projects, 'projects', errors);
}

/**
 * Validates environments.
 */
function validateEnvironments(dataset: Dataset, errors: ValidationMessage[]): void {
  dataset.environments.forEach((env, index) => {
    validateRequiredString(env.id, 'environments', index, 'id', errors);
    validateRequiredString(env.name, 'environments', index, 'name', errors);
  });
  validateUniqueIds(dataset.environments, 'environments', errors);
}

/**
 * Validates releases and their references to projects.
 */
function validateReleases(
  dataset: Dataset,
  errors: ValidationMessage[],
  warnings: ValidationMessage[]
): void {
  const projectIds = new Set(dataset.projects.map((p) => p.id));

  dataset.releases.forEach((release, index) => {
    validateRequiredString(release.id, 'releases', index, 'id', errors);
    validateRequiredString(release.projectId, 'releases', index, 'projectId', errors);
    validateDateString(release.created, 'releases', index, 'created', errors);

    // Reference validation (warning, not error)
    if (release.projectId && !projectIds.has(release.projectId)) {
      warnings.push({
        code: ErrorCodes.INVALID_REFERENCE,
        message: `Release references unknown project '${release.projectId}'`,
        path: buildJsonPath('releases', index, 'projectId'),
      });
    }
  });
  validateUniqueIds(dataset.releases, 'releases', errors);
}

/**
 * Validates deployments and their references.
 */
function validateDeployments(
  dataset: Dataset,
  errors: ValidationMessage[],
  warnings: ValidationMessage[]
): void {
  const releaseIds = new Set(dataset.releases.map((r) => r.id));
  const environmentIds = new Set(dataset.environments.map((e) => e.id));

  dataset.deployments.forEach((deployment, index) => {
    validateRequiredString(deployment.id, 'deployments', index, 'id', errors);
    validateRequiredString(deployment.releaseId, 'deployments', index, 'releaseId', errors);
    validateRequiredString(deployment.environmentId, 'deployments', index, 'environmentId', errors);
    validateDateString(deployment.deployedAt, 'deployments', index, 'deployedAt', errors);

    // Reference validations (warnings)
    if (deployment.releaseId && !releaseIds.has(deployment.releaseId)) {
      warnings.push({
        code: ErrorCodes.INVALID_REFERENCE,
        message: `Deployment references unknown release '${deployment.releaseId}'`,
        path: buildJsonPath('deployments', index, 'releaseId'),
      });
    }
    if (deployment.environmentId && !environmentIds.has(deployment.environmentId)) {
      warnings.push({
        code: ErrorCodes.INVALID_REFERENCE,
        message: `Deployment references unknown environment '${deployment.environmentId}'`,
        path: buildJsonPath('deployments', index, 'environmentId'),
      });
    }
  });
  validateUniqueIds(dataset.deployments, 'deployments', errors);
}

/**
 * Validates a dataset client-side.
 * Returns full ValidationResponse shape with deterministic ordering.
 */
export function validateDatasetClient(dataset: Dataset): ValidationResponse {
  const errors: ValidationMessage[] = [];
  const warnings: ValidationMessage[] = [];

  // Validate each collection
  validateProjects(dataset, errors);
  validateEnvironments(dataset, errors);
  validateReleases(dataset, errors, warnings);
  validateDeployments(dataset, errors, warnings);

  // Sort deterministically by (code, path, message)
  const sortedErrors = stableSort(errors, Comparators.validationMessage);
  const sortedWarnings = stableSort(warnings, Comparators.validationMessage);

  return {
    isValid: sortedErrors.length === 0,
    errors: sortedErrors,
    warnings: sortedWarnings,
    summary: {
      projectCount: dataset.projects.length,
      environmentCount: dataset.environments.length,
      releaseCount: dataset.releases.length,
      deploymentCount: dataset.deployments.length,
      errorCount: sortedErrors.length,
      warningCount: sortedWarnings.length,
    },
  };
}

/**
 * Validates that an array contains the expected collection type.
 */
function isValidArray(value: unknown): value is unknown[] {
  return Array.isArray(value);
}

/**
 * Validates basic dataset structure.
 */
export function validateDatasetStructure(data: unknown): { isValid: boolean; error?: string } {
  if (!data || typeof data !== 'object') {
    return { isValid: false, error: 'Dataset must be an object' };
  }

  const dataset = data as Record<string, unknown>;

  if (!isValidArray(dataset.projects)) {
    return { isValid: false, error: 'Dataset must contain a "projects" array' };
  }
  if (!isValidArray(dataset.environments)) {
    return { isValid: false, error: 'Dataset must contain an "environments" array' };
  }
  if (!isValidArray(dataset.releases)) {
    return { isValid: false, error: 'Dataset must contain a "releases" array' };
  }
  if (!isValidArray(dataset.deployments)) {
    return { isValid: false, error: 'Dataset must contain a "deployments" array' };
  }

  return { isValid: true };
}
