import { Helmet } from 'react-helmet-async';
import { useLocation } from 'react-router';

export default function SEO({ title, description, imageUrl, type }) {
    const location = useLocation();
    const isServer = typeof window === 'undefined';
    const baseUrl = isServer
        ? (import.meta.env.VITE_CANONICAL_BASE_URL ?? 'https://morwalpiz.it')
        : `${window.location.protocol}//${window.location.host}`;
    const currentUrl = `${baseUrl}${location.pathname}`;

    return (
        <Helmet>
            <title>{title}</title>
            <link rel="canonical" href={currentUrl} />
            <meta name='title' content={title} />
            <meta name='description' content={description} />
            <meta name='keywords' content="morwalpiz, yt, youtube, morgan walker pizzini, morgan pizzini" />
            {imageUrl?.length > 0 &&
                <link rel="image_src" href={`${baseUrl}${imageUrl}`} />
            }

            <meta property="og:type" content={type} />
            <meta property="og:title" content={title} />
            <meta property="og:description" content={description} />
            {imageUrl?.length > 0 &&
                <meta property="og:image" content={`${baseUrl}${imageUrl}`} />
            }
            <meta property="og:url" content={currentUrl} />
        </Helmet>
    );
}