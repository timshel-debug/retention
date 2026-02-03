/**
 * Export Results component and utilities.
 * [Source: docs/inputs/requirements_source.md#UI-REQ-0009-—-Export-Results]
 */

import type { EvaluationResponse } from '../../shared/types';
import { Comparators, stableSort } from '../../shared/utils';
import styles from './ExportButton.module.css';

interface ExportButtonProps {
  results: EvaluationResponse | null;
  releasesToKeep: number;
  disabled?: boolean;
}

export function ExportButton({ results, releasesToKeep, disabled }: ExportButtonProps) {
  const handleExport = () => {
    if (!results) return;

    // Sort deterministically before export
    const exportData = {
      evaluated_at: new Date().toISOString(),
      releases_to_keep: releasesToKeep,
      kept_releases: stableSort(results.keptReleases, Comparators.keptRelease),
      decisions: stableSort(results.decisions, Comparators.decision),
      diagnostics: results.diagnostics,
    };

    const json = JSON.stringify(exportData, null, 2);
    downloadJson(json, `retention-results-${formatDateForFilename(new Date())}.json`);
  };

  return (
    <button
      type="button"
      className={styles.button}
      onClick={handleExport}
      disabled={disabled || !results}
      aria-label="Export evaluation results as JSON"
    >
      📥 Export Results
    </button>
  );
}

/**
 * Downloads a JSON string as a file.
 */
function downloadJson(content: string, filename: string): void {
  const blob = new Blob([content], { type: 'application/json' });
  const url = URL.createObjectURL(blob);

  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  URL.revokeObjectURL(url);
}

/**
 * Formats a date for use in a filename (YYYY-MM-DD-HHmmss).
 */
function formatDateForFilename(date: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}-${pad(date.getHours())}${pad(date.getMinutes())}${pad(date.getSeconds())}`;
}
