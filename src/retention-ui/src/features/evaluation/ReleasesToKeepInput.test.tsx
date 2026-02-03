/**
 * Component tests for ReleasesToKeepInput.
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ReleasesToKeepInput } from './ReleasesToKeepInput';

describe('ReleasesToKeepInput', () => {
  it('renders with initial value', () => {
    render(<ReleasesToKeepInput value={5} onChange={vi.fn()} />);
    
    const input = screen.getByRole('textbox') as HTMLInputElement;
    expect(input.value).toBe('5');
  });

  it('calls onChange with new value when input changes', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    
    render(<ReleasesToKeepInput value={1} onChange={onChange} />);
    
    const input = screen.getByRole('textbox');
    await user.clear(input);
    await user.type(input, '10');
    
    expect(onChange).toHaveBeenLastCalledWith(10);
  });

  it('increments value when plus button clicked', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    
    render(<ReleasesToKeepInput value={5} onChange={onChange} />);
    
    const incrementBtn = screen.getByLabelText('Increase value');
    await user.click(incrementBtn);
    
    expect(onChange).toHaveBeenCalledWith(6);
  });

  it('decrements value when minus button clicked', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    
    render(<ReleasesToKeepInput value={5} onChange={onChange} />);
    
    const decrementBtn = screen.getByLabelText('Decrease value');
    await user.click(decrementBtn);
    
    expect(onChange).toHaveBeenCalledWith(4);
  });

  it('disables decrement at 0', () => {
    render(<ReleasesToKeepInput value={0} onChange={vi.fn()} />);
    
    const decrementBtn = screen.getByLabelText('Decrease value');
    expect(decrementBtn).toBeDisabled();
  });

  it('shows error for negative values', async () => {
    const user = userEvent.setup();
    
    render(<ReleasesToKeepInput value={1} onChange={vi.fn()} />);
    
    const input = screen.getByRole('textbox');
    await user.clear(input);
    await user.type(input, '-1');
    
    expect(screen.getByText('Number must be >= 0')).toBeInTheDocument();
  });

  it('disables input when disabled prop is true', () => {
    render(<ReleasesToKeepInput value={5} onChange={vi.fn()} disabled />);
    
    const input = screen.getByRole('textbox');
    const incrementBtn = screen.getByLabelText('Increase value');
    const decrementBtn = screen.getByLabelText('Decrease value');
    
    expect(input).toBeDisabled();
    expect(incrementBtn).toBeDisabled();
    expect(decrementBtn).toBeDisabled();
  });
});
