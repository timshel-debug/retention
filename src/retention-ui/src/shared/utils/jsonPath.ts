/**
 * JSON Path helper for producing stable paths to elements.
 * Uses JSONPath-like syntax: $.collection[index].property
 */

/**
 * Builds a JSON path string for a collection element.
 * @param collection - Collection name (e.g., 'projects')
 * @param index - Element index
 * @param property - Optional property name
 */
export function buildJsonPath(collection: string, index: number, property?: string): string {
  const basePath = `$.${collection}[${index}]`;
  return property ? `${basePath}.${property}` : basePath;
}

/**
 * Builds a JSON path for a nested property.
 */
export function buildNestedPath(segments: (string | number)[]): string {
  if (segments.length === 0) return '$';

  return (
    '$' +
    segments
      .map((seg, idx) => {
        if (typeof seg === 'number') {
          return `[${seg}]`;
        }
        return idx === 0 ? `.${seg}` : `.${seg}`;
      })
      .join('')
  );
}

/**
 * Parses a simple JSON path to extract collection and index.
 * Returns null if the path doesn't match expected format.
 */
export function parseJsonPath(path: string): { collection: string; index: number; property?: string } | null {
  const match = path.match(/^\$\.(\w+)\[(\d+)\](?:\.(\w+))?$/);
  if (!match) return null;

  return {
    collection: match[1],
    index: parseInt(match[2], 10),
    property: match[3],
  };
}
