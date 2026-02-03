/**
 * Evaluation Results Panel component.
 * [Source: docs/inputs/requirements_source.md#UI-REQ-0006-—-Run-Retention-Evaluation]
 * [Source: docs/inputs/requirements_source.md#UI-REQ-0007-—-Display-Kept-Releases]
 * [Source: docs/inputs/requirements_source.md#UI-REQ-0008-—-Display-Decision-Log]
 */

import type { EvaluationResponse, KeptRelease, Decision } from '../../shared/types';
import { Comparators, stableSort } from '../../shared/utils';
import styles from './EvaluationResults.module.css';

interface EvaluationResultsProps {
  results: EvaluationResponse | null;
  isLoading?: boolean;
}

export function EvaluationResults({ results, isLoading }: EvaluationResultsProps) {
  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <h2 className={styles.title}>Evaluation Results</h2>
          <span className={styles.loadingText}>Evaluating...</span>
        </div>
      </div>
    );
  }

  if (!results) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <h2 className={styles.title}>Evaluation Results</h2>
        </div>
        <p className={styles.placeholder}>
          Validate a dataset and click "Evaluate" to see retention results
        </p>
      </div>
    );
  }

  const { keptReleases, decisions, diagnostics } = results;

  // Sort deterministically
  const sortedKept = stableSort(keptReleases, Comparators.keptRelease);
  const sortedDecisions = stableSort(decisions, Comparators.decision);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h2 className={styles.title}>Evaluation Results</h2>
        <span className={styles.badge}>
          {keptReleases.length} release{keptReleases.length !== 1 ? 's' : ''} kept
        </span>
      </div>

      {diagnostics && (
        <div className={styles.diagnostics}>
          <span>Groups evaluated: {diagnostics.groupsEvaluated}</span>
          <span>Total kept: {diagnostics.totalKeptReleases}</span>
          {diagnostics.invalidDeploymentsExcluded > 0 && (
            <span className={styles.warningText}>
              ⚠ {diagnostics.invalidDeploymentsExcluded} invalid deployment
              {diagnostics.invalidDeploymentsExcluded !== 1 ? 's' : ''} excluded
            </span>
          )}
        </div>
      )}

      <KeptReleasesTable releases={sortedKept} />

      <DecisionLogSection decisions={sortedDecisions} />
    </div>
  );
}

interface KeptReleasesTableProps {
  releases: KeptRelease[];
}

function KeptReleasesTable({ releases }: KeptReleasesTableProps) {
  if (releases.length === 0) {
    return (
      <div className={styles.section}>
        <h3 className={styles.sectionTitle}>Kept Releases</h3>
        <p className={styles.emptyMessage}>No releases to keep</p>
      </div>
    );
  }

  return (
    <div className={styles.section}>
      <h3 className={styles.sectionTitle}>Kept Releases ({releases.length})</h3>
      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Release</th>
              <th>Project</th>
              <th>Environment</th>
              <th>Version</th>
              <th>Rank</th>
              <th>Latest Deployed</th>
            </tr>
          </thead>
          <tbody>
            {releases.map((release) => (
              <tr key={`${release.releaseId}-${release.projectId}-${release.environmentId}`}>
                <td className={styles.monoCell}>{release.releaseId}</td>
                <td className={styles.monoCell}>{release.projectId}</td>
                <td className={styles.monoCell}>{release.environmentId}</td>
                <td>{release.version}</td>
                <td className={styles.centerCell}>{release.rank}</td>
                <td>{formatDateTime(release.latestDeployedAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

interface DecisionLogSectionProps {
  decisions: Decision[];
}

function DecisionLogSection({ decisions }: DecisionLogSectionProps) {
  return (
    <details className={styles.details}>
      <summary className={styles.summary}>
        Decision Log ({decisions.length} entries)
      </summary>
      <div className={styles.decisionList}>
        {decisions.map((decision, index) => (
          <div
            key={`${decision.releaseId}-${decision.projectId}-${decision.environmentId}-${index}`}
            className={`${styles.decisionItem} ${decision.reasonCode.startsWith('kept.') ? styles.kept : styles.diagnostic}`}
          >
            <div className={styles.decisionHeader}>
              <span className={styles.decisionCode}>{decision.reasonCode}</span>
              <span className={styles.decisionRank}>
                Rank: {decision.rank} of {decision.n}
              </span>
            </div>
            <div className={styles.decisionText}>{decision.reasonText}</div>
            <div className={styles.decisionMeta}>
              <span>Release: {decision.releaseId}</span>
              <span>Project: {decision.projectId}</span>
              <span>Env: {decision.environmentId}</span>
              {decision.latestDeployedAt && (
                <span>Deployed: {formatDateTime(decision.latestDeployedAt)}</span>
              )}
            </div>
          </div>
        ))}
      </div>
    </details>
  );
}

function formatDateTime(isoString: string): string {
  try {
    const date = new Date(isoString);
    return date.toLocaleString(undefined, {
      dateStyle: 'short',
      timeStyle: 'medium',
    });
  } catch {
    return isoString;
  }
}
