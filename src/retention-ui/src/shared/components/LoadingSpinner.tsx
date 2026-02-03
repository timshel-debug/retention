/**
 * Loading spinner component.
 */

import styles from './LoadingSpinner.module.css';

interface LoadingSpinnerProps {
  size?: 'small' | 'medium' | 'large';
  label?: string;
}

export function LoadingSpinner({ size = 'medium', label = 'Loading...' }: LoadingSpinnerProps) {
  return (
    <div className={styles.container} role="status" aria-live="polite">
      <div className={`${styles.spinner} ${styles[size]}`} aria-hidden="true" />
      <span className={styles.label}>{label}</span>
    </div>
  );
}

/**
 * Inline loading indicator.
 */
interface LoadingInlineProps {
  label?: string;
}

export function LoadingInline({ label = 'Loading...' }: LoadingInlineProps) {
  return (
    <span className={styles.inline} role="status" aria-live="polite">
      <span className={styles.dots} aria-hidden="true">
        <span>.</span>
        <span>.</span>
        <span>.</span>
      </span>
      <span className="sr-only">{label}</span>
    </span>
  );
}
