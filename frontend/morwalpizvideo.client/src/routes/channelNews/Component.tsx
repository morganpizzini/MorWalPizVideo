import { Link, useLoaderData } from "react-router";
import type { ChannelNews } from "@morwalpizvideo/models";

export default function ChannelNewsDetail() {
    const item = useLoaderData() as ChannelNews;
    const image = item.images[0];

    return (
        <article className="container py-4 channel-news-detail">
            <Link to="/" className="btn btn-outline-secondary mb-3">Torna alla home</Link>
            <header className="channel-news-detail__header">
                <img src={item.channelLogoUrl || "/images/logo-150.png"} alt={item.channelName} />
                <div>
                    <div className="text-uppercase small fw-bold">{item.channelName}</div>
                    <h1>{item.title}</h1>
                    {item.subtitle && <p className="lead mb-0">{item.subtitle}</p>}
                </div>
            </header>
            {image && <img className="channel-news-detail__hero" src={image.publicUrl} alt={image.altText || item.title} />}
            <div className="channel-news-detail__body" dangerouslySetInnerHTML={{ __html: item.descriptionHtml }} />
        </article>
    );
}