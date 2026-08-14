import { Link } from 'react-router-dom';
import type { ChannelNews } from '@morwalpizvideo/models';

interface Props {
    items: ChannelNews[];
}

export default function ChannelNewsCarousel({ items }: Props) {
    if (!items.length) return null;

    return (
        <section className="channel-news-carousel" aria-label="Channel news">
            {items.map(item => (
                <Link key={item.id} to={`/channel-news/${item.id}`} className="channel-news-carousel__slide">
                    <img src={item.channelLogoUrl || '/images/logo-150.png'} alt={item.channelName} />
                    <span>
                        <small>{item.channelName}</small>
                        <strong>{item.title}</strong>
                        {item.subtitle && <em>{item.subtitle}</em>}
                    </span>
                    <i className="fa-solid fa-arrow-right" aria-hidden="true" />
                </Link>
            ))}
        </section>
    );
}