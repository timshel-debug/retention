/**
 * Validation Panel component for displaying validation results.
 * [Source: docs/inputs/requirements_source.md#UI-REQ-0004-—-Display-Validation-Results]
 */

import type { ValidationResponse, ValidationMessage } from '../../shared/types';
import { Comparators, stableSort } from '../../shared/utils';
import styles from './ValidationPanel.module.css';

interface ValidationPanelProps {
  validation: ValidationResponse | null;
  isLoading?: boolean;
}

export function ValidationPanel({ validation, isLoading }: ValidationPanelProps) {
  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <h2 className={styles.title}>Validation Results</h2>
          <span className={styles.loadingText}>Validating...</span>
        </div>
      </div>
    );
  }

  if (!validation) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <h2 className={styles.title}>Validation Results</h2>
        </div>
        <p className={styles.placeholder}>
          Paste or upload a dataset to see validation results
        </p>
      </div>
    );
  }

  const { isValid, errors, warnings, summary } = validation;

  // Sort messages deterministically
  const sortedErrors = stableSort(errors, Comparators.validationMessage);
  const sortedWarnings = stableSort(warnings, Comparators.validationMessage);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h2 className={styles.title}>Validation Results</h2>
        <StatusBadge isValid={isValid} />
      </div>

      {summary && (
        <div className={styles.diagnostics}>
          <span>Projects: {summary.projectCount}</span>
          <span>Environments: {summary.environmentCount}</span>
          <span>Releases: {summary.releaseCount}</span>
          <span>Deployments: {summary.deploymentCount}</span>
        </div>
      )}

      {sortedErrors.length > 0 && (
        <MessageList
          title="Errors"
          messages={sortedErrors}
          variant="error"
        />
      )}

      {sortedWarnings.length > 0 && (
        <MessageList
          title="Warnings"
          messages={sortedWarnings}
          variant="warning"
        />
      )}

      {isValid && sortedErrors.length === 0 && sortedWarnings.length === 0 && (
        <p className={styles.successMessage}>✓ Dataset is valid with no issues</p>
      )}
    </div>
  );
}

interface StatusBadgeProps {
  isValid: boolean;
}

function StatusBadge({ isValid }: StatusBadgeProps) {
  return (
    <span
      className={`${styles.badge} ${isValid ? styles.badgeSuccess : styles.badgeError}`}
      role="status"
    >
      {isValid ? '✓ Valid' : '✗ Invalid'}
    </span>
  );
}

interface MessageListProps {
  title: string;
  messages: ValidationMessage[];
  variant: 'error' | 'warning';
}

function MessageList({ title, messages, variant }: MessageListProps) {
  return (
    <div className={styles.messageSection}>
      <h3 className={`${styles.messageTitle} ${styles[variant]}`}>
        {title} ({messages.length})
      </h3>
      <ul className={styles.messageList} role="list">
        {messages.map((msg, index) => (
          <li
            key={`${msg.code}-${msg.path}-${index}`}
            className={`${styles.messageItem} ${styles[variant]}`}
          >
            <span className={styles.messageCode}>{msg.code}</span>
            <span className={styles.messagePath}>{msg.path}</span>
            <span className={styles.messageText}>{msg.message}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
