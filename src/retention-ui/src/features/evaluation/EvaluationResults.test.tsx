/**
 * Component tests for EvaluationResults.
 */

import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EvaluationResults } from './EvaluationResults';
import type { EvaluationResponse } from '../../shared/types';

describe('EvaluationResults', () => {
  it('shows placeholder when no results', () => {
    render(<EvaluationResults results={null} />);
    
    expect(screen.getByText(/Validate a dataset and click "Evaluate"/)).toBeInTheDocument();
  });

  it('shows loading state', () => {
    render(<EvaluationResults results={null} isLoading />);
    
    expect(screen.getByText(/Evaluating.../)).toBeInTheDocument();
  });

  it('shows kept releases count badge', () => {
    const results: EvaluationResponse = {
      keptReleases: [
        {
          releaseId: 'R1',
          projectId: 'P1',
          environmentId: 'E1',
          version: '1.0',
          created: '2024-01-01T00:00:00Z',
          latestDeployedAt: '2024-01-01T12:00:00Z',
          rank: 1,
          reasonCode: 'kept.top_n',
        },
      ],
      decisions: [
        {
          projectId: 'P1',
          environmentId: 'E1',
          releaseId: 'R1',
          n: 1,
          rank: 1,
          latestDeployedAt: '2024-01-01T12:00:00Z',
          reasonCode: 'kept.top_n',
          reasonText: 'Release R1 kept',
        },
      ],
      diagnostics: {
        groupsEvaluated: 1,
        invalidDeploymentsExcluded: 0,
        totalKeptReleases: 1,
      },
    };

    render(<EvaluationResults results={results} />);
    
    expect(screen.getByText('1 release kept')).toBeInTheDocument();
  });

  it('uses plural for multiple releases', () => {
    const results: EvaluationResponse = {
      keptReleases: [
        { releaseId: 'R1', projectId: 'P1', environmentId: 'E1', version: '1.0', created: '2024-01-01T00:00:00Z', latestDeployedAt: '2024-01-01T12:00:00Z', rank: 1, reasonCode: 'kept.top_n' },
        { releaseId: 'R2', projectId: 'P1', environmentId: 'E1', version: '2.0', created: '2024-01-01T00:00:00Z', latestDeployedAt: '2024-01-01T13:00:00Z', rank: 2, reasonCode: 'kept.top_n' },
      ],
      decisions: [],
      diagnostics: {
        groupsEvaluated: 1,
        invalidDeploymentsExcluded: 0,
        totalKeptReleases: 2,
      },
    };

    render(<EvaluationResults results={results} />);
    
    expect(screen.getByText('2 releases kept')).toBeInTheDocument();
  });

  it('shows diagnostics', () => {
    const results: EvaluationResponse = {
      keptReleases: [],
      decisions: [],
      diagnostics: {
        groupsEvaluated: 5,
        invalidDeploymentsExcluded: 2,
        totalKeptReleases: 10,
      },
    };

    render(<EvaluationResults results={results} />);
    
    expect(screen.getByText('Groups evaluated: 5')).toBeInTheDocument();
    expect(screen.getByText('Total kept: 10')).toBeInTheDocument();
    expect(screen.getByText(/2 invalid deployments excluded/)).toBeInTheDocument();
  });

  it('renders kept releases table', () => {
    const results: EvaluationResponse = {
      keptReleases: [
        {
          releaseId: 'Release-1',
          projectId: 'Project-A',
          environmentId: 'Staging',
          version: '1.0.0',
          created: '2024-01-01T00:00:00Z',
          latestDeployedAt: '2024-01-15T10:30:00Z',
          rank: 1,
          reasonCode: 'kept.top_n',
        },
      ],
      decisions: [],
      diagnostics: {
        groupsEvaluated: 1,
        invalidDeploymentsExcluded: 0,
        totalKeptReleases: 1,
      },
    };

    render(<EvaluationResults results={results} />);
    
    expect(screen.getByText('Release-1')).toBeInTheDocument();
    expect(screen.getByText('Project-A')).toBeInTheDocument();
    expect(screen.getByText('Staging')).toBeInTheDocument();
    expect(screen.getByText('1.0.0')).toBeInTheDocument();
  });

  it('shows empty message when no releases kept', () => {
    const results: EvaluationResponse = {
      keptReleases: [],
      decisions: [],
      diagnostics: {
        groupsEvaluated: 1,
        invalidDeploymentsExcluded: 0,
        totalKeptReleases: 0,
      },
    };

    render(<EvaluationResults results={results} />);
    
    expect(screen.getByText('No releases to keep')).toBeInTheDocument();
  });

  it('renders decision log as collapsible details', () => {
    const results: EvaluationResponse = {
      keptReleases: [],
      decisions: [
        {
          projectId: 'P1',
          environmentId: 'E1',
          releaseId: 'R1',
          n: 1,
          rank: 1,
          latestDeployedAt: '2024-01-01T00:00:00Z',
          reasonCode: 'kept.top_n',
          reasonText: 'Release R1 kept because it is rank 1 of 1',
        },
      ],
      diagnostics: {
        groupsEvaluated: 1,
        invalidDeploymentsExcluded: 0,
        totalKeptReleases: 0,
      },
    };

    render(<EvaluationResults results={results} />);
    
    expect(screen.getByText('Decision Log (1 entries)')).toBeInTheDocument();
  });
});
