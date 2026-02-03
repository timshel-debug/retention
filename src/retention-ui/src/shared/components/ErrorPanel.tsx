/**
 * Error Panel component for displaying ProblemDetails errors.
 * [Source: docs/inputs/requirements_source.md#UI-REQ-0010-—-Error-UX-ProblemDetails]
 */

import type { ApiResult } from '../types';
import { getErrorMessage, getErrorActionHint } from '../api';
import styles from './ErrorPanel.module.css';

interface ErrorPanelProps {
  result: ApiResult<unknown>;
  onDismiss?: () => void;
}

export function ErrorPanel({ result, onDismiss }: ErrorPanelProps) {
  if (result.kind === 'ok') return null;

  const message = getErrorMessage(result);
  const hint = getErrorActionHint(result);
  const isNetwork = result.kind === 'network';
  const problem = result.kind === 'problem' ? result.problem : null;

  return (
    <div
      className={`${styles.errorPanel} ${isNetwork ? styles.networkError : styles.serverError}`}
      role="alert"
      aria-live="polite"
    >
      <div className={styles.header}>
        <span className={styles.icon} aria-hidden="true">
          {isNetwork ? '🌐' : '⚠️'}
        </span>
        <span className={styles.title}>
          {isNetwork ? 'Network Error' : `Error ${problem?.status || ''}`}
        </span>
        {onDismiss && (
          <button
            className={styles.dismissButton}
            onClick={onDismiss}
            aria-label="Dismiss error"
          >
            ×
          </button>
        )}
      </div>

      <div className={styles.message}>{message}</div>

      {hint && <div className={styles.hint}>{hint}</div>}

      {problem?.error_code && (
        <div className={styles.metadata}>
          <span className={styles.errorCode}>Code: {problem.error_code}</span>
          {problem.trace_id && (
            <span className={styles.traceId}>Trace: {problem.trace_id}</span>
          )}
        </div>
      )}
    </div>
  );
}

/**
 * Simple error alert for inline messages.
 */
interface ErrorAlertProps {
  message: string;
  className?: string;
}

export function ErrorAlert({ message, className }: ErrorAlertProps) {
  return (
    <div className={`${styles.errorAlert} ${className || ''}`} role="alert">
      {message}
    </div>
  );
}
