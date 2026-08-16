import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import RichTextEditor, { createColumnLayout } from './RichTextEditor';

describe('RichTextEditor', () => {
  it('creates sanitizer-compatible column layouts', () => {
    expect(createColumnLayout(2)).toBe('<div class="page-columns"><div class="page-column"><p><br></p></div><div class="page-column"><p><br></p></div></div>');
  });

  it('inserts a selected column layout into the persisted HTML', () => {
    const onChange = vi.fn();

    render(<RichTextEditor value="<p>Existing</p>" onChange={onChange} />);
    fireEvent.click(screen.getByRole('button', { name: 'Insert 3 column layout' }));

    expect(onChange).toHaveBeenCalledWith(expect.stringContaining('class="page-columns"'));
    expect(onChange.mock.lastCall?.[0]).toContain('class="page-column"');
    expect(onChange.mock.lastCall?.[0].match(/class="page-column"/g)).toHaveLength(3);
  });
});