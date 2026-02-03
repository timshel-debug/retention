/**
 * Stable sorting utilities with explicit tie-breakers.
 * Ensures deterministic output regardless of input order.
 * [Source: docs/inputs/requirements_source.md#UI-NFR-0004-—-Determinism]
 */

/**
 * Comparator function type.
 */
export type Comparator<T> = (a: T, b: T) => number;

/**
 * Creates a string comparator using ordinal comparison.
 */
export function compareString(a: string, b: string): number {
  if (a < b) return -1;
  if (a > b) return 1;
  return 0;
}

/**
 * Creates a numeric comparator.
 */
export function compareNumber(a: number, b: number): number {
  return a - b;
}

/**
 * Creates an ISO date string comparator.
 */
export function compareDateString(a: string, b: string): number {
  return new Date(a).getTime() - new Date(b).getTime();
}

/**
 * Chains multiple comparators to create tie-breakers.
 * Returns the first non-zero comparison result.
 */
export function chainComparators<T>(...comparators: Comparator<T>[]): Comparator<T> {
  return (a: T, b: T) => {
    for (const comparator of comparators) {
      const result = comparator(a, b);
      if (result !== 0) return result;
    }
    return 0;
  };
}

/**
 * Creates a comparator from a key selector function.
 */
export function compareBy<T, K>(
  selector: (item: T) => K,
  compare: (a: K, b: K) => number
): Comparator<T> {
  return (a: T, b: T) => compare(selector(a), selector(b));
}

/**
 * Creates a descending comparator from an existing comparator.
 */
export function descending<T>(comparator: Comparator<T>): Comparator<T> {
  return (a: T, b: T) => -comparator(a, b);
}

/**
 * Performs a stable sort on an array using the provided comparator.
 * Returns a new array; does not mutate the original.
 * Uses manual stable sort with index preservation for cross-browser compatibility.
 */
export function stableSort<T>(items: readonly T[], comparator: Comparator<T>): T[] {
  // Manual stable sort with index preservation
  // This ensures stable sorting across all environments
  const indexed = items.map((item, index) => ({ item, index }));
  indexed.sort((a, b) => {
    const result = comparator(a.item, b.item);
    if (result !== 0) return result;
    return a.index - b.index; // Preserve original order for ties
  });
  return indexed.map(({ item }) => item);
}

/**
 * Pre-built comparators for common use cases.
 */
export const Comparators = {
  /**
   * Compares ValidationMessages by (code, path, message).
   */
  validationMessage: chainComparators<{ code: string; path: string | null; message: string }>(
    compareBy((m) => m.code, compareString),
    compareBy((m) => m.path ?? '', compareString),
    compareBy((m) => m.message, compareString)
  ),

  /**
   * Compares KeptReleases by (projectId, environmentId, rank, releaseId).
   */
  keptRelease: chainComparators<{
    projectId: string;
    environmentId: string;
    rank: number;
    releaseId: string;
  }>(
    compareBy((r) => r.projectId, compareString),
    compareBy((r) => r.environmentId, compareString),
    compareBy((r) => r.rank, compareNumber),
    compareBy((r) => r.releaseId, compareString)
  ),

  /**
   * Compares Decisions by (kindSort, projectId, environmentId, rank, reasonCode, releaseId).
   */
  decision: chainComparators<{
    reasonCode: string;
    projectId: string;
    environmentId: string;
    rank: number;
    releaseId: string;
  }>(
    compareBy((d) => (d.reasonCode.startsWith('kept.') ? 0 : 1), compareNumber),
    compareBy((d) => d.projectId, compareString),
    compareBy((d) => d.environmentId, compareString),
    compareBy((d) => d.rank, compareNumber),
    compareBy((d) => d.reasonCode, compareString),
    compareBy((d) => d.releaseId, compareString)
  ),
};
