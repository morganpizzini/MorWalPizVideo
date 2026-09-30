import { describe, expect, it } from 'vitest';
import { Button, Dropdown } from 'react-bootstrap';
import { render } from '../../test/test-utils';
import PageHeader from './index';

describe('PageHeader', () => {
  it('keeps action controls and the create link in one non-wrapping controls region', () => {
    const { container } = render(
      <PageHeader
        title="Products"
        createLink="./create"
        actions={
          <>
            <Button>Import</Button>
            <Dropdown>
              <Dropdown.Toggle>Actions</Dropdown.Toggle>
              <Dropdown.Menu />
            </Dropdown>
          </>
        }
      />
    );

    const controls = container.querySelector('.overflow-auto');
    const createLink = container.querySelector('a[href="/create"]');

    expect(controls).toHaveClass('d-flex', 'flex-grow-1', 'flex-nowrap');
    expect(createLink?.parentElement).toBe(controls);
    expect(controls?.querySelector('.dropdown')).not.toBeNull();
  });
});
