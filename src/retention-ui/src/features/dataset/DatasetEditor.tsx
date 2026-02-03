/**
 * Dataset Editor component with JSON textarea and file upload.
 * [Source: docs/inputs/requirements_source.md#UI-REQ-0001-—-Paste-Dataset-JSON]
 * [Source: docs/inputs/requirements_source.md#UI-REQ-0002-—-Upload-Dataset-JSON]
 */

import { useState, useCallback, useRef, ChangeEvent } from 'react';
import type { Dataset } from '../../shared/types';
import { validateDatasetStructure } from '../validation/clientValidation';
import { ErrorAlert } from '../../shared/components';
import styles from './DatasetEditor.module.css';

interface DatasetEditorProps {
  value: string;
  onChange: (text: string, dataset: Dataset | null, parseError: string | null) => void;
}

export function DatasetEditor({ value, onChange }: DatasetEditorProps) {
  const [parseError, setParseError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const parseAndValidate = useCallback(
    (text: string) => {
      if (!text.trim()) {
        setParseError(null);
        onChange(text, null, null);
        return;
      }

      try {
        const parsed = JSON.parse(text);
        const structureResult = validateDatasetStructure(parsed);

        if (!structureResult.isValid) {
          setParseError(structureResult.error || 'Invalid dataset structure');
          onChange(text, null, structureResult.error || 'Invalid dataset structure');
          return;
        }

        setParseError(null);
        onChange(text, parsed as Dataset, null);
      } catch (err) {
        const errorMessage =
          err instanceof SyntaxError
            ? `JSON parse error: ${err.message}`
            : 'Invalid JSON format';
        setParseError(errorMessage);
        onChange(text, null, errorMessage);
      }
    },
    [onChange]
  );

  const handleTextChange = useCallback(
    (e: ChangeEvent<HTMLTextAreaElement>) => {
      const text = e.target.value;
      parseAndValidate(text);
    },
    [parseAndValidate]
  );

  const handleFileUpload = useCallback(
    (e: ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      if (!file) return;

      // Validate file type
      if (!file.name.endsWith('.json')) {
        setParseError('Please upload a .json file');
        return;
      }

      const reader = new FileReader();
      reader.onload = (event) => {
        const text = event.target?.result as string;
        parseAndValidate(text);
      };
      reader.onerror = () => {
        setParseError('Failed to read file');
      };
      reader.readAsText(file);

      // Reset input so the same file can be uploaded again
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    },
    [parseAndValidate]
  );

  const handleUploadClick = useCallback(() => {
    fileInputRef.current?.click();
  }, []);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h2 className={styles.title}>Dataset Input</h2>
        <div className={styles.actions}>
          <button
            type="button"
            onClick={handleUploadClick}
            className={styles.uploadButton}
            aria-label="Upload JSON file"
          >
            📁 Upload .json
          </button>
          <input
            ref={fileInputRef}
            type="file"
            accept=".json"
            onChange={handleFileUpload}
            className={styles.fileInput}
            aria-hidden="true"
          />
        </div>
      </div>

      <label htmlFor="dataset-editor" className={styles.label}>
        Paste or upload dataset JSON:
      </label>

      <textarea
        id="dataset-editor"
        className={`${styles.textarea} ${parseError ? styles.hasError : ''}`}
        value={value}
        onChange={handleTextChange}
        placeholder={`{
  "projects": [...],
  "environments": [...],
  "releases": [...],
  "deployments": [...]
}`}
        spellCheck={false}
        aria-invalid={!!parseError}
        aria-describedby={parseError ? 'parse-error' : undefined}
      />

      {parseError && (
        <div id="parse-error">
          <ErrorAlert message={parseError} />
        </div>
      )}
    </div>
  );
}
