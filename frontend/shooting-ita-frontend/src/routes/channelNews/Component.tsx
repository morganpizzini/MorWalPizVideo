import { Link, useLoaderData } from 'react-router-dom';
import type { ChannelNews } from '@morwalpizvideo/models';

export default function ChannelNewsDetail() {
    const item = useLoaderData() as ChannelNews;
    const hero = item.images[0];

    return (
        <article className="channel-news-detail">
            <Link to="/" className="btn btn-outline-light mb-3">Back to home</Link>
            <header className="channel-news-detail__header">
                <img src={item.channelLogoUrl || '/images/logo-150.png'} alt={item.channelName} />
                <div>
                    <small>{item.channelName}</small>
                    <h1>{item.title}</h1>
                    {item.subtitle && <p>{item.subtitle}</p>}
                </div>
            </header>
            {hero && <img className="channel-news-detail__hero" src={hero.publicUrl} alt={hero.altText || item.title} />}
            <div className="channel-news-detail__body" dangerouslySetInnerHTML={{ __html: item.descriptionHtml }} />
        </article>
    );
}