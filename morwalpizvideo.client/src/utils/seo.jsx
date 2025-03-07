import { Helmet } from 'react-helmet-async';
export default function SEO({ title, description, imageUrl,type }) {
    return (
        <Helmet>
            <title>{title}</title>
            <link rel="canonical" href={window.location.href} />
            <meta name='title' content={title} />
            <meta name='description' content={description} />
            <meta name='keywords' content="morwalpiz, yt, youtube, morgan walker pizzini, morgan pizzini" />
            <link rel="image_src" href={`${location.protocol + '//' + location.host}${imageUrl}`} />

            <meta property="og:type" content={type} />
            <meta property="og:title" content={title} />
            <meta property="og:description" content={description} />
            <meta property="og:image" content={imageUrl} />
            <meta property="og:url" content={window.location.href} />
        </Helmet>
    )
}