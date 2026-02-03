/**
 * Component tests for ErrorPanel.
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ErrorPanel } from './ErrorPanel';
import type { ApiResult } from '../types';

describe('ErrorPanel', () => {
  it('renders nothing for ok result', () => {
    const okResult: ApiResult<unknown> = { kind: 'ok', data: {} };
    
    const { container } = render(<ErrorPanel result={okResult} />);
    
    expect(container).toBeEmptyDOMElement();
  });

  it('shows network error for network result', () => {
    const networkResult: ApiResult<unknown> = {
      kind: 'network',
      error: new Error('Connection failed'),
    };
    
    render(<ErrorPanel result={networkResult} />);
    
    expect(screen.getByText('Network Error')).toBeInTheDocument();
    expect(screen.getByText(/Connection failed/)).toBeInTheDocument();
  });

  it('shows server error for problem result', () => {
    const problemResult: ApiResult<unknown> = {
      kind: 'problem',
      problem: {
        type: '/errors/validation',
        title: 'Validation Error',
        status: 400,
        detail: 'Invalid input data',
        error_code: 'validation.invalid',
      },
    };
    
    render(<ErrorPanel result={problemResult} />);
    
    expect(screen.getByText('Error 400')).toBeInTheDocument();
    expect(screen.getByText(/Invalid input data/)).toBeInTheDocument();
    // error_code appears in both message and metadata
    expect(screen.getAllByText(/validation\.invalid/)).toHaveLength(2);
  });

  it('shows trace_id when present', () => {
    const problemResult: ApiResult<unknown> = {
      kind: 'problem',
      problem: {
        title: 'Error',
        status: 500,
        error_code: 'server.error',
        trace_id: 'abc123xyz',
      },
    };
    
    render(<ErrorPanel result={problemResult} />);
    
    expect(screen.getByText(/abc123xyz/)).toBeInTheDocument();
  });

  it('calls onDismiss when dismiss button clicked', () => {
    const onDismiss = vi.fn();
    const networkResult: ApiResult<unknown> = {
      kind: 'network',
      error: new Error('Connection failed'),
    };
    
    render(<ErrorPanel result={networkResult} onDismiss={onDismiss} />);
    
    const dismissBtn = screen.getByLabelText('Dismiss error');
    fireEvent.click(dismissBtn);
    
    expect(onDismiss).toHaveBeenCalled();
  });

  it('does not show dismiss button when onDismiss not provided', () => {
    const networkResult: ApiResult<unknown> = {
      kind: 'network',
      error: new Error('Connection failed'),
    };
    
    render(<ErrorPanel result={networkResult} />);
    
    expect(screen.queryByLabelText('Dismiss error')).not.toBeInTheDocument();
  });

  it('has role="alert" for accessibility', () => {
    const networkResult: ApiResult<unknown> = {
      kind: 'network',
      error: new Error('Test error'),
    };
    
    render(<ErrorPanel result={networkResult} />);
    
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });
});
