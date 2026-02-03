/**
 * Unit tests for stable sort utilities.
 */

import { describe, it, expect } from 'vitest';
import {
  stableSort,
  compareString,
  compareNumber,
  compareBy,
  chainComparators,
  descending,
  Comparators,
} from './sorting';
import type { ValidationMessage, KeptRelease, Decision } from '../types';

describe('compareString', () => {
  it('returns negative when a < b', () => {
    expect(compareString('apple', 'banana')).toBeLessThan(0);
  });

  it('returns positive when a > b', () => {
    expect(compareString('cherry', 'banana')).toBeGreaterThan(0);
  });

  it('returns zero when a === b', () => {
    expect(compareString('test', 'test')).toBe(0);
  });

  it('uses ordinal comparison (case-sensitive)', () => {
    // Uppercase letters come before lowercase in ASCII
    expect(compareString('Apple', 'apple')).toBeLessThan(0);
  });
});

describe('compareNumber', () => {
  it('returns negative when a < b', () => {
    expect(compareNumber(1, 5)).toBeLessThan(0);
  });

  it('returns positive when a > b', () => {
    expect(compareNumber(10, 5)).toBeGreaterThan(0);
  });

  it('returns zero when a === b', () => {
    expect(compareNumber(42, 42)).toBe(0);
  });
});

describe('compareBy', () => {
  it('compares by extracted key', () => {
    const items = [{ name: 'charlie' }, { name: 'alice' }, { name: 'bob' }];
    const comparator = compareBy<{ name: string }, string>((x) => x.name, compareString);

    const sorted = [...items].sort(comparator);

    expect(sorted.map((x) => x.name)).toEqual(['alice', 'bob', 'charlie']);
  });
});

describe('chainComparators', () => {
  it('uses secondary comparator when primary returns 0', () => {
    const items = [
      { name: 'alice', age: 30 },
      { name: 'alice', age: 25 },
      { name: 'bob', age: 20 },
    ];

    const comparator = chainComparators<{ name: string; age: number }>(
      compareBy((x) => x.name, compareString),
      compareBy((x) => x.age, compareNumber)
    );

    const sorted = [...items].sort(comparator);

    expect(sorted).toEqual([
      { name: 'alice', age: 25 },
      { name: 'alice', age: 30 },
      { name: 'bob', age: 20 },
    ]);
  });
});

describe('descending', () => {
  it('reverses comparator result', () => {
    const numbers = [3, 1, 4, 1, 5, 9];
    const sorted = [...numbers].sort(descending(compareNumber));

    expect(sorted).toEqual([9, 5, 4, 3, 1, 1]);
  });
});

describe('stableSort', () => {
  it('does not mutate the original array', () => {
    const original = [3, 1, 2];
    const sorted = stableSort(original, compareNumber);

    expect(original).toEqual([3, 1, 2]);
    expect(sorted).toEqual([1, 2, 3]);
  });

  it('preserves relative order of equal elements (stability)', () => {
    const items = [
      { key: 'a', order: 1 },
      { key: 'b', order: 2 },
      { key: 'a', order: 3 },
      { key: 'b', order: 4 },
    ];

    // Sort only by key - should preserve original order for same keys
    const sorted = stableSort(items, compareBy((x) => x.key, compareString));

    expect(sorted).toEqual([
      { key: 'a', order: 1 },
      { key: 'a', order: 3 },
      { key: 'b', order: 2 },
      { key: 'b', order: 4 },
    ]);
  });

  it('returns empty array for empty input', () => {
    const result = stableSort([], compareNumber);
    expect(result).toEqual([]);
  });
});

describe('Comparators.validationMessage', () => {
  it('sorts by code, then path, then message (REQ: deterministic ordering)', () => {
    const messages: ValidationMessage[] = [
      { code: 'b', path: 'a', message: 'z' },
      { code: 'a', path: 'b', message: 'y' },
      { code: 'a', path: 'a', message: 'x' },
      { code: 'a', path: 'a', message: 'w' },
    ];

    const sorted = stableSort(messages, Comparators.validationMessage);

    expect(sorted).toEqual([
      { code: 'a', path: 'a', message: 'w' },
      { code: 'a', path: 'a', message: 'x' },
      { code: 'a', path: 'b', message: 'y' },
      { code: 'b', path: 'a', message: 'z' },
    ]);
  });

  it('handles null paths (sorted as empty string)', () => {
    const messages: ValidationMessage[] = [
      { code: 'a', path: 'b', message: 'x' },
      { code: 'a', path: null, message: 'y' },
    ];

    const sorted = stableSort(messages, Comparators.validationMessage);

    // null path treated as empty string, sorts before 'b'
    expect(sorted[0].path).toBeNull();
    expect(sorted[1].path).toBe('b');
  });
});

describe('Comparators.keptRelease', () => {
  it('sorts by projectId, then environmentId, then rank', () => {
    const releases: KeptRelease[] = [
      { projectId: 'P2', environmentId: 'E1', rank: 1, releaseId: 'R1', version: '1.0', created: '2024-01-01', latestDeployedAt: '2024-01-01', reasonCode: 'kept.top_n' },
      { projectId: 'P1', environmentId: 'E2', rank: 2, releaseId: 'R2', version: '1.0', created: '2024-01-01', latestDeployedAt: '2024-01-01', reasonCode: 'kept.top_n' },
      { projectId: 'P1', environmentId: 'E1', rank: 2, releaseId: 'R3', version: '1.0', created: '2024-01-01', latestDeployedAt: '2024-01-01', reasonCode: 'kept.top_n' },
      { projectId: 'P1', environmentId: 'E1', rank: 1, releaseId: 'R4', version: '1.0', created: '2024-01-01', latestDeployedAt: '2024-01-01', reasonCode: 'kept.top_n' },
    ];

    const sorted = stableSort(releases, Comparators.keptRelease);

    expect(sorted.map((r) => r.releaseId)).toEqual(['R4', 'R3', 'R2', 'R1']);
  });
});

describe('Comparators.decision', () => {
  it('sorts by projectId, then environmentId, then rank, then releaseId', () => {
    const decisions: Decision[] = [
      { projectId: 'P1', environmentId: 'E1', rank: 1, releaseId: 'R2', n: 1, latestDeployedAt: '2024-01-01', reasonCode: 'kept.top_n', reasonText: 'test' },
      { projectId: 'P1', environmentId: 'E1', rank: 1, releaseId: 'R1', n: 1, latestDeployedAt: '2024-01-01', reasonCode: 'kept.top_n', reasonText: 'test' },
    ];

    const sorted = stableSort(decisions, Comparators.decision);

    // Same project, env, rank - should sort by releaseId
    expect(sorted.map((d) => d.releaseId)).toEqual(['R1', 'R2']);
  });
});
