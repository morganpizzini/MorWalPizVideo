import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useLoaderData } from 'react-router-dom';
import { subscribeNewsletter } from '@morwalpizvideo/services';
import { useGoogleReCaptcha } from 'react19-google-recaptcha-v3';
import ChannelNewsDetail from '../routes/channelNews/Component';

vi.mock('react-router-dom', () => ({
    Link: ({ children }: { children: React.ReactNode }) => <a href="/">{children}</a>,
    useLoaderData: vi.fn(),
}));

vi.mock('@morwalpizvideo/services', () => ({
    subscribeNewsletter: vi.fn(),
}));

vi.mock('react19-google-recaptcha-v3', () => ({
    useGoogleReCaptcha: vi.fn(),
}));

const channelNews = {
    id: 'news-1',
    slug: 'news',
    channelId: 'channel-from-item',
    channelName: 'Shooting ITA',
    channelLogoUrl: '',
    title: 'Channel news',
    subtitle: '',
    descriptionHtml: '<p>News</p>',
    images: [],
    status: 'Published' as const,
};

describe('ChannelNewsDetail newsletter', () => {
    beforeEach(() => {
        vi.mocked(useLoaderData).mockReturnValue(channelNews);
        vi.mocked(useGoogleReCaptcha).mockReturnValue({
            executeRecaptcha: vi.fn().mockResolvedValue('recaptcha-token'),
        });
        vi.mocked(subscribeNewsletter).mockResolvedValue({});
    });

    it('opens the form and subscribes the channel from the loaded item', async () => {
        render(<ChannelNewsDetail />);

        fireEvent.click(screen.getByRole('button', { name: /iscriviti alla newsletter/i }));
        fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'person@example.com' } });
        fireEvent.change(screen.getByLabelText('Lingua'), { target: { value: 'ENG' } });
        fireEvent.click(screen.getByRole('button', { name: /conferma iscrizione/i }));

        await waitFor(() => expect(subscribeNewsletter).toHaveBeenCalledWith({
            channelId: 'channel-from-item',
            email: 'person@example.com',
            language: 'ENG',
            recaptchaToken: 'recaptcha-token',
        }));
        expect(screen.getByRole('status')).toHaveTextContent(/controlla la posta/i);
    });

    it('shows an error when subscription fails', async () => {
        vi.mocked(subscribeNewsletter).mockRejectedValue(new Error('network failure'));
        render(<ChannelNewsDetail />);

        fireEvent.click(screen.getByRole('button', { name: /iscriviti alla newsletter/i }));
        fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'person@example.com' } });
        fireEvent.click(screen.getByRole('button', { name: /conferma iscrizione/i }));

        expect(await screen.findByRole('alert')).toHaveTextContent(/impossibile inviare/i);
    });
});