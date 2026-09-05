import { getPublicFaq } from '@morwalpizvideo/services';
import type { LoaderFunction } from 'react-router-dom';

export const faqLoader: LoaderFunction = ({ request }) => {
    const category = new URL(request.url).searchParams.get('category') ?? undefined;
    return getPublicFaq(category);
};