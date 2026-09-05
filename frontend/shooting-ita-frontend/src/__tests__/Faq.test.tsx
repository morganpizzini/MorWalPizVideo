import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useLoaderData } from 'react-router-dom';
import { voteFaqAnswer } from '@morwalpizvideo/services';
import FaqPage from '../routes/faq/Component';

vi.mock('react-router-dom', () => ({ useLoaderData: vi.fn() }));
vi.mock('@morwalpizvideo/services', () => ({ voteFaqAnswer: vi.fn() }));

const faq = [{
    id: 'faq-1', question: 'How?', categorySlug: 'general', categoryName: 'General',
    answers: [{ channelName: 'Alpha', content: 'Carefully.', helpfulVotes: 2, notHelpfulVotes: 0 }],
}];

describe('FAQ public page', () => {
    beforeEach(() => {
        vi.mocked(useLoaderData).mockReturnValue(faq);
        vi.mocked(voteFaqAnswer).mockResolvedValue({ accepted: true, changed: false, helpfulVotes: 3, notHelpfulVotes: 0 });
    });

    it('renders published answer data and votes by FAQ and channel', async () => {
        render(<FaqPage />);
        fireEvent.click(screen.getByRole('button', { name: 'Utile' }));
        await waitFor(() => expect(voteFaqAnswer).toHaveBeenCalledWith('faq-1', 'Alpha', 1));
        expect(screen.getByRole('status')).toHaveTextContent(/registrato/i);
    });
});
