/**
 * ReleasesToKeep input component.
 * [Source: docs/inputs/requirements_source.md#UI-REQ-0005-—-Specify-Number-of-Releases-to-Keep]
 */

import { useState, useCallback, ChangeEvent } from 'react';
import styles from './ReleasesToKeepInput.module.css';

interface ReleasesToKeepInputProps {
  value: number;
  onChange: (value: number) => void;
  disabled?: boolean;
}

export function ReleasesToKeepInput({
  value,
  onChange,
  disabled = false,
}: ReleasesToKeepInputProps) {
  const [inputValue, setInputValue] = useState<string>(String(value));
  const [error, setError] = useState<string | null>(null);

  const handleChange = useCallback(
    (e: ChangeEvent<HTMLInputElement>) => {
      const text = e.target.value;
      setInputValue(text);

      // Allow empty for typing
      if (text === '') {
        setError('Number is required');
        return;
      }

      const num = parseInt(text, 10);

      if (isNaN(num)) {
        setError('Please enter a valid number');
        return;
      }

      if (num < 0) {
        setError('Number must be >= 0');
        return;
      }

      setError(null);
      onChange(num);
    },
    [onChange]
  );

  const handleBlur = useCallback(() => {
    // On blur, sync the input value with the actual value
    if (inputValue === '' || error) {
      setInputValue(String(value));
      setError(null);
    }
  }, [inputValue, value, error]);

  const handleIncrement = useCallback(() => {
    if (disabled) return;
    const newValue = value + 1;
    setInputValue(String(newValue));
    setError(null);
    onChange(newValue);
  }, [value, onChange, disabled]);

  const handleDecrement = useCallback(() => {
    if (disabled || value <= 0) return;
    const newValue = value - 1;
    setInputValue(String(newValue));
    setError(null);
    onChange(newValue);
  }, [value, onChange, disabled]);

  return (
    <div className={styles.container}>
      <label htmlFor="releases-to-keep" className={styles.label}>
        Releases to keep per project/environment:
      </label>
      <div className={styles.inputGroup}>
        <button
          type="button"
          className={styles.spinButton}
          onClick={handleDecrement}
          disabled={disabled || value <= 0}
          aria-label="Decrease value"
        >
          −
        </button>
        <input
          id="releases-to-keep"
          type="text"
          inputMode="numeric"
          pattern="[0-9]*"
          className={`${styles.input} ${error ? styles.hasError : ''}`}
          value={inputValue}
          onChange={handleChange}
          onBlur={handleBlur}
          disabled={disabled}
          aria-invalid={!!error}
          aria-describedby={error ? 'rtk-error' : undefined}
        />
        <button
          type="button"
          className={styles.spinButton}
          onClick={handleIncrement}
          disabled={disabled}
          aria-label="Increase value"
        >
          +
        </button>
      </div>
      {error && (
        <span id="rtk-error" className={styles.errorText}>
          {error}
        </span>
      )}
    </div>
  );
}
