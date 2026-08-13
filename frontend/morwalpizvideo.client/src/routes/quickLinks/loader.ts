import type { LoaderFunctionArgs } from 'react-router';
import { getQuickLinks } from '../../services/quickLinks';

function isApiError(value: unknown): value is { errors: unknown[]; status?: number } {
    return typeof value === 'object' && value !== null &&
        Array.isArray((value as { errors?: unknown[] }).errors);
}

function formatError(error: unknown): string {
    if (typeof error === 'string') return error;
    if (typeof error === 'object' && error !== null && 'message' in error) {
        const message = (error as { message?: unknown }).message;
        if (typeof message === 'string') return message;
    }
    return String(error);
}

export default async function loader({ params }: LoaderFunctionArgs) {
    const url = params.url?.trim();
    if (!url) throw new Response('QuickLinks not found', { status: 404 });

    const response = await getQuickLinks(url);
    if (isApiError(response)) {
        const message = response.errors.map(formatError).filter(Boolean).join('\n') || 'QuickLinks not found';
        throw new Response(message, { status: response.status ?? 404 });
    }

    return response;
}