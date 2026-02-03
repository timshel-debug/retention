/**
 * API Client for Release Retention Service.
 * Implements discriminated union result type for ok/problem/network states.
 * [Source: docs/inputs/requirements_source.md#UI-REQ-0004-—-Server-Side-Validation]
 */

import type {
  ApiResult,
  Dataset,
  EvaluateRetentionRequest,
  EvaluationResponse,
  ProblemDetails,
  ValidateDatasetRequest,
  ValidationResponse,
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api/v1';

/**
 * Determines if a response is a ProblemDetails response.
 */
function isProblemDetails(
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  data: any,
  contentType: string | null
): data is ProblemDetails {
  return (
    contentType?.includes('application/problem+json') ||
    (typeof data === 'object' && data !== null && 'status' in data && 'title' in data)
  );
}

/**
 * Makes an API request and returns a discriminated union result.
 */
async function apiRequest<T>(
  endpoint: string,
  options: RequestInit
): Promise<ApiResult<T>> {
  try {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json, application/problem+json',
        ...options.headers,
      },
    });

    const contentType = response.headers.get('Content-Type');
    const text = await response.text();
    const data = text ? JSON.parse(text) : null;

    if (!response.ok) {
      if (isProblemDetails(data, contentType)) {
        return { kind: 'problem', problem: data };
      }
      // Construct a ProblemDetails from non-standard error
      return {
        kind: 'problem',
        problem: {
          title: 'Request Failed',
          status: response.status,
          detail: text || response.statusText,
        },
      };
    }

    return { kind: 'ok', data: data as T };
  } catch (error) {
    // Network errors (fetch failure, timeout, etc.)
    return {
      kind: 'network',
      error: error instanceof Error ? error : new Error(String(error)),
    };
  }
}

/**
 * API client methods.
 */
export const ApiClient = {
  /**
   * Validates a dataset against server-side rules.
   * POST /api/v1/datasets/validate
   */
  async validateDataset(
    dataset: Dataset,
    correlationId?: string
  ): Promise<ApiResult<ValidationResponse>> {
    const request: ValidateDatasetRequest = {
      dataset,
      correlationId,
    };

    return apiRequest<ValidationResponse>('/datasets/validate', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  /**
   * Evaluates retention policy for a dataset.
   * POST /api/v1/retention/evaluate
   */
  async evaluateRetention(
    dataset: Dataset,
    releasesToKeep: number,
    correlationId?: string
  ): Promise<ApiResult<EvaluationResponse>> {
    const request: EvaluateRetentionRequest = {
      dataset,
      releasesToKeep,
      correlationId,
    };

    return apiRequest<EvaluationResponse>('/retention/evaluate', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },
};

/**
 * Helper to extract user-friendly error message from API result.
 */
export function getErrorMessage(result: ApiResult<unknown>): string {
  switch (result.kind) {
    case 'ok':
      return '';
    case 'problem': {
      const { problem } = result;
      const code = problem.error_code ? ` (${problem.error_code})` : '';
      return `${problem.title}${code}: ${problem.detail || 'Unknown error'}`;
    }
    case 'network':
      return `Network error: ${result.error.message}. Please check your connection and try again.`;
  }
}

/**
 * Helper to get user action hint for error states.
 */
export function getErrorActionHint(result: ApiResult<unknown>): string {
  switch (result.kind) {
    case 'ok':
      return '';
    case 'problem': {
      const { problem } = result;
      if (problem.status >= 500) {
        return 'This is a server error. Please try again later or contact support.';
      }
      if (problem.status === 429) {
        return 'Rate limit exceeded. Please wait a moment before trying again.';
      }
      if (problem.status === 400) {
        return 'Please check your input and try again.';
      }
      return 'Please verify your request and try again.';
    }
    case 'network':
      return 'Check your internet connection and try again.';
  }
}
