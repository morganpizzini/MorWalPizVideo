import { describe, expect, it } from 'vitest';
import { useState } from 'react';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import TagInput from '../TagInput';
import { normalizeTags } from '../TagInput/tagRules';

function Harness({ initial = [], suggestions = [] }: { initial?: string[]; suggestions?: string[] }) {
  const [tags, setTags] = useState<string[]>(initial);
  return <TagInput label="Tags" value={tags} onChange={setTags} suggestions={suggestions} />;
}

const getField = () => screen.getByRole('combobox');

describe('normalizeTags', () => {
  it('trims, drops empty values and dedupes case-insensitively keeping the first casing', () => {
    expect(normalizeTags(['  Tutorial ', '', '   ', 'tutorial', 'TUTORIAL', 'Gear'])).toEqual([
      'Tutorial',
      'Gear',
    ]);
  });
});

describe('TagInput', () => {
  it('creates a tag on Enter and trims whitespace', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.type(getField(), '  Tutorial  {Enter}');

    expect(screen.getByText('Tutorial')).toBeInTheDocument();
    expect(getField()).toHaveValue('');
  });

  it('creates a tag on comma', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.type(getField(), 'Gear,');

    expect(screen.getByText('Gear')).toBeInTheDocument();
  });

  it('creates several tags from a comma separated burst', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.type(getField(), 'Gear,Optics,');

    expect(screen.getByText('Gear')).toBeInTheDocument();
    expect(screen.getByText('Optics')).toBeInTheDocument();
  });

  it('ignores empty values', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.type(getField(), '   {Enter}');
    await user.type(getField(), ',');

    expect(screen.queryAllByRole('button', { name: /^Remove tag/ })).toHaveLength(0);
  });

  it('dedupes case-insensitively and keeps the first display casing', async () => {
    const user = userEvent.setup();
    render(<Harness initial={['Tutorial']} />);

    await user.type(getField(), 'tutorial{Enter}');

    expect(screen.getAllByText(/tutorial/i)).toHaveLength(1);
    expect(screen.getByText('Tutorial')).toBeInTheDocument();
  });

  it('removes a tag with its remove button', async () => {
    const user = userEvent.setup();
    render(<Harness initial={['Tutorial', 'Gear']} />);

    await user.click(screen.getByRole('button', { name: 'Remove tag Tutorial' }));

    expect(screen.queryByText('Tutorial')).not.toBeInTheDocument();
    expect(screen.getByText('Gear')).toBeInTheDocument();
  });

  it('removes the last tag with Backspace on an empty field', async () => {
    const user = userEvent.setup();
    render(<Harness initial={['Tutorial', 'Gear']} />);

    await user.click(getField());
    await user.keyboard('{Backspace}');

    expect(screen.queryByText('Gear')).not.toBeInTheDocument();
    expect(screen.getByText('Tutorial')).toBeInTheDocument();
  });

  it('exposes suggestions as an accessible listbox and selects with the keyboard', async () => {
    const user = userEvent.setup();
    render(<Harness suggestions={['Tutorial', 'Turbo']} />);

    const field = getField();
    expect(field).toHaveAttribute('aria-expanded', 'false');

    await user.type(field, 'tu');

    const listbox = screen.getByRole('listbox', { name: 'Tag suggestions' });
    expect(field).toHaveAttribute('aria-expanded', 'true');
    expect(within(listbox).getAllByRole('option')).toHaveLength(2);

    await user.keyboard('{ArrowDown}');
    expect(field).toHaveAttribute('aria-activedescendant');

    await user.keyboard('{Enter}');
    expect(screen.getByRole('button', { name: 'Remove tag Tutorial' })).toBeInTheDocument();
  });

  it('hides already selected tags from the suggestion list', async () => {
    const user = userEvent.setup();
    render(<Harness initial={['Tutorial']} suggestions={['Tutorial', 'Turbo']} />);

    await user.type(getField(), 'tu');

    const options = within(screen.getByRole('listbox')).getAllByRole('option');
    expect(options.map(option => option.textContent)).toEqual(['Turbo']);
  });

  it('rejects a tag longer than the maximum length', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    // maxLength on the input prevents overlong entry entirely.
    expect(getField()).toHaveAttribute('maxLength', '32');

    await user.type(getField(), `${'a'.repeat(40)}{Enter}`);

    expect(screen.getByRole('button', { name: /^Remove tag a{32}$/ })).toBeInTheDocument();
  });

  it('blocks adding beyond the tag limit', async () => {
    const user = userEvent.setup();
    const initial = Array.from({ length: 20 }, (_, index) => `tag-${index}`);
    render(<Harness initial={initial} />);

    const field = getField();
    expect(field).toBeDisabled();

    await user.type(field, 'extra{Enter}');
    expect(screen.queryByText('extra')).not.toBeInTheDocument();
  });
});
