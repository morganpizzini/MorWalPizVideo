import { Helmet } from 'react-helmet-async';
import { useLocation } from 'react-router';

interface SEOProps {
  title: string;
  description: string;
  imageUrl?: string;
  type: string;
  socialTitle?: string;
  socialDescription?: string;
}

export default function SEO({
  title,
  description,
  imageUrl,
  type,
  socialTitle = title,
  socialDescription = description,
}: SEOProps) {
  const location = useLocation();
  const baseUrl = import.meta.env.SSR
    ? (import.meta.env.VITE_CANONICAL_BASE_URL ?? 'https://morwalpiz.com')
    : `${window.location.protocol}//${window.location.host}`;
  const currentUrl = `${baseUrl}${location.pathname}`;
  const resolvedImageUrl = imageUrl ? new URL(imageUrl, baseUrl).href : undefined;

  return (
    <Helmet>
      <title>{title}</title>
      <link rel="canonical" href={currentUrl} />
      <meta name="title" content={title} />
      <meta name="description" content={description} />
      <meta
        name="keywords"
        content="morwalpiz, yt, youtube, morgan walker pizzini, morgan pizzini"
      />
      {imageUrl && imageUrl.length > 0 && <link rel="image_src" href={resolvedImageUrl} />}

      <meta property="og:type" content={type} />
      <meta property="og:title" content={socialTitle} />
      <meta property="og:description" content={socialDescription} />
      {imageUrl && imageUrl.length > 0 && <meta property="og:image" content={resolvedImageUrl} />}
      <meta property="og:url" content={currentUrl} />
      <meta name="twitter:card" content="summary_large_image" />
      <meta name="twitter:title" content={socialTitle} />
      <meta name="twitter:description" content={socialDescription} />
      {imageUrl && imageUrl.length > 0 && (
        <meta name="twitter:image" content={resolvedImageUrl} />
      )}
    </Helmet>
  );
}
