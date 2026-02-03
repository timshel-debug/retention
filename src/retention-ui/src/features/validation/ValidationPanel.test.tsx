/**
 * Component tests for ValidationPanel.
 */

import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ValidationPanel } from './ValidationPanel';
import type { ValidationResponse } from '../../shared/types';

describe('ValidationPanel', () => {
  it('shows placeholder when no validation', () => {
    render(<ValidationPanel validation={null} />);
    
    expect(screen.getByText(/Paste or upload a dataset/)).toBeInTheDocument();
  });

  it('shows loading state', () => {
    render(<ValidationPanel validation={null} isLoading />);
    
    expect(screen.getByText(/Validating.../)).toBeInTheDocument();
  });

  it('shows valid badge for valid dataset', () => {
    const validation: ValidationResponse = {
      isValid: true,
      errors: [],
      warnings: [],
      summary: {
        projectCount: 1,
        environmentCount: 1,
        releaseCount: 1,
        deploymentCount: 1,
        errorCount: 0,
        warningCount: 0,
      },
    };

    render(<ValidationPanel validation={validation} />);
    
    expect(screen.getByRole('status')).toHaveTextContent('✓ Valid');
  });

  it('shows invalid badge when errors present', () => {
    const validation: ValidationResponse = {
      isValid: false,
      errors: [{ code: 'validation.error', message: 'Test error', path: '$.test' }],
      warnings: [],
      summary: {
        projectCount: 1,
        environmentCount: 1,
        releaseCount: 1,
        deploymentCount: 1,
        errorCount: 1,
        warningCount: 0,
      },
    };

    render(<ValidationPanel validation={validation} />);
    
    expect(screen.getByRole('status')).toHaveTextContent('✗ Invalid');
  });

  it('displays error messages', () => {
    const validation: ValidationResponse = {
      isValid: false,
      errors: [
        { code: 'validation.missing_required_field', message: 'id is required', path: '$.projects[0].id' },
      ],
      warnings: [],
      summary: {
        projectCount: 1,
        environmentCount: 0,
        releaseCount: 0,
        deploymentCount: 0,
        errorCount: 1,
        warningCount: 0,
      },
    };

    render(<ValidationPanel validation={validation} />);
    
    expect(screen.getByText('Errors (1)')).toBeInTheDocument();
    expect(screen.getByText('validation.missing_required_field')).toBeInTheDocument();
    expect(screen.getByText('$.projects[0].id')).toBeInTheDocument();
    expect(screen.getByText('id is required')).toBeInTheDocument();
  });

  it('displays warning messages', () => {
    const validation: ValidationResponse = {
      isValid: true,
      errors: [],
      warnings: [
        { code: 'validation.invalid_reference', message: 'Unknown project', path: '$.releases[0].projectId' },
      ],
      summary: {
        projectCount: 1,
        environmentCount: 1,
        releaseCount: 1,
        deploymentCount: 0,
        errorCount: 0,
        warningCount: 1,
      },
    };

    render(<ValidationPanel validation={validation} />);
    
    expect(screen.getByText('Warnings (1)')).toBeInTheDocument();
    expect(screen.getByText('validation.invalid_reference')).toBeInTheDocument();
  });

  it('shows summary counts', () => {
    const validation: ValidationResponse = {
      isValid: true,
      errors: [],
      warnings: [],
      summary: {
        projectCount: 2,
        environmentCount: 3,
        releaseCount: 5,
        deploymentCount: 10,
        errorCount: 0,
        warningCount: 0,
      },
    };

    render(<ValidationPanel validation={validation} />);
    
    expect(screen.getByText('Projects: 2')).toBeInTheDocument();
    expect(screen.getByText('Environments: 3')).toBeInTheDocument();
    expect(screen.getByText('Releases: 5')).toBeInTheDocument();
    expect(screen.getByText('Deployments: 10')).toBeInTheDocument();
  });

  it('shows success message for valid dataset with no issues', () => {
    const validation: ValidationResponse = {
      isValid: true,
      errors: [],
      warnings: [],
      summary: {
        projectCount: 1,
        environmentCount: 1,
        releaseCount: 1,
        deploymentCount: 1,
        errorCount: 0,
        warningCount: 0,
      },
    };

    render(<ValidationPanel validation={validation} />);
    
    expect(screen.getByText(/Dataset is valid with no issues/)).toBeInTheDocument();
  });
});
