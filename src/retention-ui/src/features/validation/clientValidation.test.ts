/**
 * Unit tests for client-side validation.
 */

import { describe, it, expect } from 'vitest';
import { validateDatasetClient, validateDatasetStructure } from './clientValidation';
import type { Dataset } from '../../shared/types';

describe('validateDatasetStructure', () => {
  it('rejects non-object input', () => {
    expect(validateDatasetStructure(null)).toEqual({
      isValid: false,
      error: 'Dataset must be an object',
    });
    expect(validateDatasetStructure('string')).toEqual({
      isValid: false,
      error: 'Dataset must be an object',
    });
  });

  it('rejects missing arrays', () => {
    expect(validateDatasetStructure({})).toEqual({
      isValid: false,
      error: 'Dataset must contain a "projects" array',
    });

    expect(validateDatasetStructure({ projects: [] })).toEqual({
      isValid: false,
      error: 'Dataset must contain an "environments" array',
    });

    expect(validateDatasetStructure({ projects: [], environments: [] })).toEqual({
      isValid: false,
      error: 'Dataset must contain a "releases" array',
    });

    expect(validateDatasetStructure({ projects: [], environments: [], releases: [] })).toEqual({
      isValid: false,
      error: 'Dataset must contain a "deployments" array',
    });
  });

  it('accepts valid structure', () => {
    expect(
      validateDatasetStructure({
        projects: [],
        environments: [],
        releases: [],
        deployments: [],
      })
    ).toEqual({ isValid: true });
  });
});

describe('validateDatasetClient', () => {
  const emptyDataset: Dataset = {
    projects: [],
    environments: [],
    releases: [],
    deployments: [],
  };

  it('returns valid for empty dataset', () => {
    const result = validateDatasetClient(emptyDataset);

    expect(result.isValid).toBe(true);
    expect(result.errors).toHaveLength(0);
    expect(result.warnings).toHaveLength(0);
  });

  it('validates required string fields', () => {
    const dataset: Dataset = {
      projects: [{ id: '', name: 'Test' }],
      environments: [],
      releases: [],
      deployments: [],
    };

    const result = validateDatasetClient(dataset);

    expect(result.isValid).toBe(false);
    expect(result.errors).toContainEqual(
      expect.objectContaining({
        code: 'validation.missing_required_field',
        path: '$.projects[0].id',
      })
    );
  });

  it('validates unique IDs', () => {
    const dataset: Dataset = {
      projects: [
        { id: 'P1', name: 'Project 1' },
        { id: 'P1', name: 'Project 1 Duplicate' },
      ],
      environments: [],
      releases: [],
      deployments: [],
    };

    const result = validateDatasetClient(dataset);

    expect(result.isValid).toBe(false);
    expect(result.errors).toContainEqual(
      expect.objectContaining({
        code: 'validation.duplicate_id',
        path: '$.projects[1].id',
        message: expect.stringContaining('Duplicate ID'),
      })
    );
  });

  it('validates date strings', () => {
    const dataset: Dataset = {
      projects: [{ id: 'P1', name: 'Test' }],
      environments: [],
      releases: [{ id: 'R1', projectId: 'P1', created: 'not-a-date', version: null }],
      deployments: [],
    };

    const result = validateDatasetClient(dataset);

    expect(result.isValid).toBe(false);
    expect(result.errors).toContainEqual(
      expect.objectContaining({
        code: 'validation.invalid_type',
        path: '$.releases[0].created',
        message: expect.stringContaining('ISO 8601'),
      })
    );
  });

  it('warns about invalid references (not errors)', () => {
    const dataset: Dataset = {
      projects: [{ id: 'P1', name: 'Test' }],
      environments: [{ id: 'E1', name: 'Prod' }],
      releases: [{ id: 'R1', projectId: 'MISSING', created: '2024-01-01T00:00:00Z', version: '1.0' }],
      deployments: [],
    };

    const result = validateDatasetClient(dataset);

    // Invalid reference is a warning, not an error, so dataset is still "valid"
    // (no structural errors)
    expect(result.isValid).toBe(true);
    expect(result.warnings).toContainEqual(
      expect.objectContaining({
        code: 'validation.invalid_reference',
        path: '$.releases[0].projectId',
        message: expect.stringContaining('unknown project'),
      })
    );
  });

  it('returns errors in deterministic order', () => {
    const dataset: Dataset = {
      projects: [
        { id: '', name: '' },
        { id: '', name: '' },
      ],
      environments: [],
      releases: [],
      deployments: [],
    };

    const result1 = validateDatasetClient(dataset);
    const result2 = validateDatasetClient(dataset);

    // Results should be identical
    expect(result1.errors).toEqual(result2.errors);

    // Errors should be sorted by (code, path, message)
    const paths = result1.errors.map((e) => e.path);
    const sortedPaths = [...paths].sort();
    expect(paths).toEqual(sortedPaths);
  });

  it('includes summary counts', () => {
    const dataset: Dataset = {
      projects: [{ id: 'P1', name: 'Test' }],
      environments: [{ id: 'E1', name: 'Prod' }],
      releases: [
        { id: 'R1', projectId: 'P1', created: '2024-01-01T00:00:00Z', version: '1.0' },
        { id: 'R2', projectId: 'P1', created: '2024-01-01T00:00:00Z', version: '2.0' },
      ],
      deployments: [
        { id: 'D1', releaseId: 'R1', environmentId: 'E1', deployedAt: '2024-01-01T00:00:00Z' },
      ],
    };

    const result = validateDatasetClient(dataset);

    expect(result.summary).toEqual({
      projectCount: 1,
      environmentCount: 1,
      releaseCount: 2,
      deploymentCount: 1,
      errorCount: 0,
      warningCount: 0,
    });
  });
});
