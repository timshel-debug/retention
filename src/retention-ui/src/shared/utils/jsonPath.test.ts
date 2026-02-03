/**
 * Unit tests for JSON path builder.
 */

import { describe, it, expect } from 'vitest';
import { buildJsonPath } from './jsonPath';

describe('buildJsonPath', () => {
  it('builds path for collection item property', () => {
    const path = buildJsonPath('projects', 0, 'id');
    expect(path).toBe('$.projects[0].id');
  });

  it('handles different indices', () => {
    expect(buildJsonPath('releases', 5, 'version')).toBe('$.releases[5].version');
    expect(buildJsonPath('deployments', 99, 'deployedAt')).toBe('$.deployments[99].deployedAt');
  });

  it('handles various property names', () => {
    expect(buildJsonPath('releases', 0, 'projectId')).toBe('$.releases[0].projectId');
    expect(buildJsonPath('environments', 1, 'name')).toBe('$.environments[1].name');
  });
});
