/**
 * Main App component for the Release Retention Console.
 * Orchestrates dataset input, validation, evaluation, and export.
 */

import { useState, useCallback, useMemo } from 'react';
import type {
  Dataset,
  ValidationResponse,
  EvaluationResponse,
  ApiResult,
} from './shared/types';
import { ApiClient } from './shared/api';
import { ErrorPanel } from './shared/components';
import { DatasetEditor } from './features/dataset';
import { ValidationPanel, validateDatasetClient } from './features/validation';
import {
  ReleasesToKeepInput,
  EvaluationResults,
  ExportButton,
} from './features/evaluation';
import styles from './App.module.css';

type WorkflowState =
  | 'idle'
  | 'validating'
  | 'validated'
  | 'evaluating'
  | 'evaluated'
  | 'error';

export default function App() {
  // Dataset state
  const [datasetText, setDatasetText] = useState('');
  const [dataset, setDataset] = useState<Dataset | null>(null);
  const [parseError, setParseError] = useState<string | null>(null);

  // Workflow state
  const [workflowState, setWorkflowState] = useState<WorkflowState>('idle');

  // Validation state
  const [validationResult, setValidationResult] = useState<
    ApiResult<ValidationResponse> | null
  >(null);

  // Evaluation state
  const [releasesToKeep, setReleasesToKeep] = useState(1);
  const [evaluationResult, setEvaluationResult] = useState<
    ApiResult<EvaluationResponse> | null
  >(null);

  // Derived state
  const validationResponse = useMemo(() => {
    if (validationResult?.kind === 'ok') {
      return validationResult.data;
    }
    return null;
  }, [validationResult]);

  const evaluationResponse = useMemo(() => {
    if (evaluationResult?.kind === 'ok') {
      return evaluationResult.data;
    }
    return null;
  }, [evaluationResult]);

  const isValidForEvaluation = useMemo(() => {
    return validationResponse?.isValid === true && !parseError;
  }, [validationResponse, parseError]);

  // Handlers
  const handleDatasetChange = useCallback(
    (text: string, parsedDataset: Dataset | null, error: string | null) => {
      setDatasetText(text);
      setDataset(parsedDataset);
      setParseError(error);

      // Clear validation and evaluation when dataset changes
      setValidationResult(null);
      setEvaluationResult(null);
      setWorkflowState('idle');

      // Run client-side validation immediately if we have a valid dataset
      if (parsedDataset) {
        const clientValidation = validateDatasetClient(parsedDataset);
        setValidationResult({ kind: 'ok', data: clientValidation });
      }
    },
    []
  );

  const handleValidate = useCallback(async () => {
    if (!dataset) return;

    setWorkflowState('validating');
    setEvaluationResult(null);

    try {
      const result = await ApiClient.validateDataset(dataset);
      setValidationResult(result);
      setWorkflowState(result.kind === 'ok' ? 'validated' : 'error');
    } catch {
      setWorkflowState('error');
    }
  }, [dataset]);

  const handleEvaluate = useCallback(async () => {
    if (!dataset || !isValidForEvaluation) return;

    setWorkflowState('evaluating');

    try {
      const result = await ApiClient.evaluateRetention(dataset, releasesToKeep);
      setEvaluationResult(result);
      setWorkflowState(result.kind === 'ok' ? 'evaluated' : 'error');
    } catch {
      setWorkflowState('error');
    }
  }, [dataset, releasesToKeep, isValidForEvaluation]);

  const handleDismissError = useCallback(() => {
    if (evaluationResult?.kind !== 'ok') {
      setEvaluationResult(null);
    }
    if (validationResult?.kind !== 'ok') {
      setValidationResult(null);
    }
    setWorkflowState(validationResponse ? 'validated' : 'idle');
  }, [evaluationResult, validationResult, validationResponse]);

  // Show the most recent error
  const currentError = useMemo(() => {
    if (evaluationResult && evaluationResult.kind !== 'ok') {
      return evaluationResult;
    }
    if (validationResult && validationResult.kind !== 'ok') {
      return validationResult;
    }
    return null;
  }, [evaluationResult, validationResult]);

  return (
    <div className={styles.app}>
      <header className={styles.header}>
        <h1 className={styles.title}>Release Retention Console</h1>
        <p className={styles.subtitle}>
          Evaluate which releases to keep based on deployment history
        </p>
      </header>

      <main className={styles.main}>
        {currentError && (
          <ErrorPanel result={currentError} onDismiss={handleDismissError} />
        )}

        <div className={styles.grid}>
          <section className={styles.inputSection}>
            <DatasetEditor value={datasetText} onChange={handleDatasetChange} />

            <div className={styles.controls}>
              <ReleasesToKeepInput
                value={releasesToKeep}
                onChange={setReleasesToKeep}
                disabled={workflowState === 'evaluating' || workflowState === 'validating'}
              />

              <div className={styles.actions}>
                <button
                  type="button"
                  className={styles.validateButton}
                  onClick={handleValidate}
                  disabled={!dataset || workflowState === 'validating'}
                >
                  {workflowState === 'validating' ? 'Validating...' : '✓ Validate'}
                </button>

                <button
                  type="button"
                  className={styles.evaluateButton}
                  onClick={handleEvaluate}
                  disabled={!isValidForEvaluation || workflowState === 'evaluating'}
                >
                  {workflowState === 'evaluating' ? 'Evaluating...' : '▶ Evaluate'}
                </button>

                <ExportButton
                  results={evaluationResponse}
                  releasesToKeep={releasesToKeep}
                  disabled={!evaluationResponse}
                />
              </div>
            </div>
          </section>

          <section className={styles.resultsSection}>
            <ValidationPanel
              validation={validationResponse}
              isLoading={workflowState === 'validating'}
            />

            <EvaluationResults
              results={evaluationResponse}
              isLoading={workflowState === 'evaluating'}
            />
          </section>
        </div>
      </main>

      <footer className={styles.footer}>
        <p>
          <a
            href="https://github.com/OctopusDeploy"
            target="_blank"
            rel="noopener noreferrer"
          >
            Octopus Deploy
          </a>{' '}
          · Release Retention Service
        </p>
      </footer>
    </div>
  );
}
