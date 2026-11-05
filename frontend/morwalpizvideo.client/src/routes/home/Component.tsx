import { Link, useLoaderData, useRevalidator, useSearchParams } from 'react-router';
import { useEffect, useRef, useState } from 'react';
import { FacebookShareButton, FacebookIcon, WhatsappShareButton, WhatsappIcon } from 'react-share';
import type { BlogSummary, ChannelNews, Survey } from '@morwalpizvideo/models';
import SEO from '@utils/seo';
import configKeys from '@utils/configKeys';
import { trackPageView } from '../../analytics/consent';
import SocialLinks from '../../components/SocialLinks';
import './style.scss';

export interface IndexMatch {
  contentId: string;
  title?: string;
  description?: string;
  category?: string;
  categories: { title: string }[];
  tags?: string[];
  videoRefs?: { youtubeId: string }[];
  images?: { publicUrl: string; altText?: string }[];
  videos?: { youtubeId: string }[];
  shortLinks: { target: string; code: string }[];
  creationDateTime?: string;
  url?: string;
}

interface SponsorItem {
  id: string;
  title: string;
  imgSrc: string;
  url: string;
}

export interface IndexData {
  matches: IndexMatch[];
  articles: BlogSummary[];
  articlesUnavailable: boolean;
  configuration: Record<string, boolean>;
  surveys: Survey[];
  channelNews: ChannelNews[];
  sponsors: SponsorItem[];
  error: boolean;
}

export type EditorialItem =
  | { type: 'video' | 'collection'; match: IndexMatch }
  | { type: 'article'; article: BlogSummary };
type ContentFilter = 'all' | EditorialItem['type'];

const contentFilters: { value: ContentFilter; label: string }[] = [
  { value: 'all', label: 'Tutti' },
  { value: 'video', label: 'Video' },
  { value: 'article', label: 'Articoli' },
  { value: 'collection', label: 'Serie' },
];

export function getMatchDestination(match: IndexMatch): string {
  if ((match.videoRefs?.length ?? 0) > 1) return `/matches/${match.url || match.contentId}`;
  const shortLink = match.shortLinks?.find((link) => link.target === match.contentId)?.code;
  if (!shortLink) return `https://youtu.be/${match.contentId}`;
  return shortLink.startsWith('http') ? shortLink : `https://shorts.morwalpiz.com/${shortLink}`;
}

function itemDate(item: EditorialItem): string {
  return item.type === 'article' ? item.article.publishedAt : item.match.creationDateTime ?? '';
}

function itemTags(item: EditorialItem): string[] {
  return item.type === 'article' ? item.article.tags : item.match.tags ?? [];
}

function itemCategories(item: EditorialItem): string[] {
  return item.type === 'article' ? [] : item.match.categories?.map((category) => category.title) ?? [];
}

function itemTitle(item: EditorialItem): string {
  return item.type === 'article' ? item.article.title : item.match.title || 'Video MorWalPiz';
}

function itemDescription(item: EditorialItem): string {
  return item.type === 'article' ? item.article.summary : item.match.description ?? '';
}

function itemImage(item: EditorialItem): string {
  return item.type === 'article'
    ? item.article.coverUrl || '/images/logo-300.png'
    : item.match.images?.[0]?.publicUrl || `https://img.youtube.com/vi/${item.match.contentId}/hqdefault.jpg`;
}

function itemDestination(item: EditorialItem): string {
  return item.type === 'article' ? `/blog/${item.article.slug}` : getMatchDestination(item.match);
}

export function createEditorialFeed(matches: IndexMatch[], articles: BlogSummary[]): EditorialItem[] {
  const items: EditorialItem[] = [
    ...matches.filter((match) => match.videoRefs != null).map((match): EditorialItem => ({
      type: match.videoRefs!.length > 1 ? 'collection' : 'video',
      match,
    })),
    ...articles.map((article): EditorialItem => ({ type: 'article', article })),
  ];
  return items.sort((left, right) => (Date.parse(itemDate(right)) || 0) - (Date.parse(itemDate(left)) || 0));
}

export function filterEditorialFeed(
  items: EditorialItem[],
  type: string,
  categories: string[],
  tags: string[],
  search: string
): EditorialItem[] {
  const query = search.trim().toLocaleLowerCase('it');
  return items.filter((item) =>
    (type === 'all' || type === item.type) &&
    categories.every((category) => itemCategories(item).includes(category)) &&
    tags.every((tag) => itemTags(item).includes(tag)) &&
    (!query || `${itemTitle(item)} ${itemDescription(item)} ${itemTags(item).join(' ')}`.toLocaleLowerCase('it').includes(query))
  );
}

export default function Index() {
  const data = useLoaderData() as IndexData;
  const { revalidate } = useRevalidator();
  const hasSentPageView = useRef(false);
  const [params, setParams] = useSearchParams();
  const [categories, setCategories] = useState<string[]>([]);
  const [tags, setTags] = useState<string[]>([]);
  const type = contentFilters.some((filter) => filter.value === params.get('type')) ? params.get('type')! : 'all';
  const search = params.get('q') ?? '';
  const feed = createEditorialFeed(data.matches, data.articles ?? []);
  const filtered = filterEditorialFeed(feed, type, categories, tags, search);
  const allCategories = [...new Set(feed.flatMap(itemCategories))];
  const allTags = [...new Set(feed.flatMap(itemTags))].sort((left, right) => left.localeCompare(right));
  const availableCategories = new Set(filtered.flatMap(itemCategories));
  const availableTags = new Set(filtered.flatMap(itemTags));
  const hasFilters = type !== 'all' || categories.length > 0 || tags.length > 0 || search.length > 0;
  const featured = feed[0];

  useEffect(() => {
    if (!hasSentPageView.current) {
      trackPageView('home', '/');
      hasSentPageView.current = true;
    }
  }, []);

  function changeType(value: ContentFilter) {
    setParams((current) => {
      if (value === 'all') current.delete('type');
      else current.set('type', value);
      return current;
    }, { preventScrollReset: true });
  }

  function clearFilters() {
    setCategories([]);
    setTags([]);
    setParams({}, { preventScrollReset: true });
  }

  function toggle(value: string, selected: string[], update: (values: string[]) => void) {
    update(selected.includes(value) ? selected.filter((item) => item !== value) : [...selected, value]);
  }

  return (
    <main className="magazine-home">
      <SEO title="MorWalPiz" description="Video, serie e articoli di Morgan Pizzini. Tiro sportivo, gare e vita sul campo." imageUrl={featured ? itemImage(featured) : '/images/logo-300.png'} type="website" />
      <h1 className="visually-hidden">MorWalPiz: video, serie e articoli</h1>
      {data.error ? (
        <div className="home-empty" role="alert">
          <h2>I contenuti non sono disponibili al momento.</h2>
          <button type="button" className="editorial-button" onClick={() => revalidate()}>Riprova <i className="fas fa-redo" aria-hidden="true" /></button>
        </div>
      ) : (
        <>
          {data.configuration[configKeys.STREAM_ENABLE] && (
            <div className="home-alert" role="status"><span><i className="fas fa-broadcast-tower" aria-hidden="true" /> Una diretta è in corso!</span><Link to="/stream">Vai alla diretta <i className="fas fa-arrow-right" aria-hidden="true" /></Link></div>
          )}
          {data.surveys.map((survey) => (
            <div key={survey.id} className="home-alert"><span>Nuovo sondaggio: {survey.title}</span><Link to={`/survey/${survey.url}`}>Rispondi ora <i className="fas fa-arrow-right" aria-hidden="true" /></Link></div>
          ))}
          <section className="home-feature" aria-label="In evidenza">
            {featured ? <FeaturedContent item={featured} /> : <div className="home-empty"><h2>Nuovi contenuti, presto.</h2><p>Al momento non ci sono contenuti disponibili. Torna più tardi!</p></div>}
            <aside className="home-feature__aside">
              <div className="journey-panel"><h2>Segui il percorso</h2><p>Ci vediamo anche qui.</p><SocialLinks /></div>
              <DonationCard />
            </aside>
          </section>
          {data.channelNews[0] && <ChannelNewsBanner item={data.channelNews[0]} />}
          <section id="latest-content" className="latest-content" aria-labelledby="latest-heading">
            <div className="section-toolbar">
              <h2 id="latest-heading" className="editorial-heading">Ultimi contenuti</h2>
              <div className="type-filters" role="group" aria-label="Filtra per tipo di contenuto">
                {contentFilters.map((filter) => <button key={filter.value} type="button" className="filter-option" aria-pressed={type === filter.value} onClick={() => changeType(filter.value)}>{filter.label}</button>)}
              </div>
              <a className="editorial-text-link" href="https://www.youtube.com/@morwalpiz/videos" target="_blank" rel="noopener noreferrer">Tutti i video <i className="fas fa-arrow-right" aria-hidden="true" /></a>
            </div>
            {(allCategories.length > 0 || allTags.length > 0 || search) && (
              <div className="content-filters">
                {search && <p className="search-result-label">Risultati per: <strong>{search}</strong></p>}
                {allCategories.length > 0 && <div className="filter-scroll" role="group" aria-label="Filtra per categoria">{allCategories.map((category) => <button key={category} type="button" className="filter-option" aria-pressed={categories.includes(category)} disabled={!categories.includes(category) && !availableCategories.has(category)} onClick={() => toggle(category, categories, setCategories)}>{category}</button>)}</div>}
                {allTags.length > 0 && <div className="filter-scroll" role="group" aria-label="Filtra per tag">{allTags.map((tag) => <button key={tag} type="button" className="filter-option filter-option--tag" aria-pressed={tags.includes(tag)} disabled={!tags.includes(tag) && !availableTags.has(tag)} onClick={() => toggle(tag, tags, setTags)}>#{tag}</button>)}</div>}
              </div>
            )}
            {hasFilters && <div className="filter-summary" role="status"><span>{filtered.length} contenuti</span><button type="button" onClick={clearFilters}>Rimuovi filtri <i className="fas fa-times" aria-hidden="true" /></button></div>}
            {!filtered.length && <div className="home-empty" role="status"><h3>{hasFilters ? 'Nessun contenuto trovato.' : 'Nessun contenuto pubblicato.'}</h3>{hasFilters && <button className="editorial-button editorial-button--outline" onClick={clearFilters}>Rimuovi filtri</button>}</div>}
            <div className="editorial-grid">
              {filtered.slice(0, 4).map((item) => <ContentCard key={item.type === 'article' ? `article-${item.article.slug}` : `match-${item.match.url || item.match.contentId}`} item={item} />)}
              {!hasFilters && filtered.length > 0 && <ShortsCard />}
            </div>
          </section>
          <div className="sponsors-and-gear px-1">
            <Sponsors sponsors={data.sponsors} />
            <Link to="/attrezzatura" className="gear-banner"><i className="fas fa-tag" aria-hidden="true" /><span><strong>Attrezzatura & accessori</strong><small>Quello che porto sul campo.</small></span><span className="editorial-button editorial-button--outline">Scopri <i className="fas fa-arrow-right" aria-hidden="true" /></span></Link>
          </div>
          <div className="home-editorial-bottom">
            <section className="latest-articles" aria-labelledby="articles-heading">
              <div className="section-toolbar"><h2 id="articles-heading" className="editorial-heading">Ultimi articoli</h2><Link className="editorial-text-link" to="/blog">Tutti gli articoli <i className="fas fa-arrow-right" aria-hidden="true" /></Link></div>
              {data.articlesUnavailable ? <p className="home-section-message" role="status">Gli articoli non sono disponibili al momento. <Link to="/blog">Vai al blog</Link></p> : !data.articles?.length ? <p className="home-section-message">Nessun articolo pubblicato.</p> : <div className="article-grid">{data.articles.slice(0, 3).map((article) => <ContentCard key={article.slug} item={{ type: 'article', article }} />)}</div>}
            </section>
            <section className="community-banner" aria-labelledby="community-heading"><div><h2 id="community-heading">Entra nella community</h2><p>Ci si incontra sul campo. E non solo.</p><SocialLinks /></div></section>
          </div>
          {filtered.length > 4 && <section className="more-content" aria-label="Altri contenuti"><h2 className="editorial-heading">Ancora da scoprire</h2><div className="editorial-grid">{filtered.slice(4).map((item) => <ContentCard key={item.type === 'article' ? `article-${item.article.slug}` : `match-${item.match.url || item.match.contentId}`} item={item} />)}</div></section>}
        </>
      )}
    </main>
  );
}

function ContentBadge({ type }: { type: EditorialItem['type'] }) {
  return <span className="content-badge"><i className={`fas ${type === 'collection' ? 'fa-layer-group' : type === 'article' ? 'fa-file-alt' : 'fa-play'}`} aria-hidden="true" />{type === 'collection' ? 'Serie' : type === 'article' ? 'Articolo' : 'Video'}</span>;
}

function FeaturedContent({ item }: { item: EditorialItem }) {
  return (
    <article className="featured-content">
      <img className="featured-content__image" src={itemImage(item)} alt={item.type === 'article' ? item.article.coverAlt : itemTitle(item)} fetchPriority="high" />
      {item.type === 'collection' && <span className="episode-count"><i className="fas fa-layer-group" aria-hidden="true" />{item.match.videoRefs?.length} episodi</span>}
      <div className="featured-content__copy"><ContentBadge type={item.type} /><h2>{itemTitle(item)}</h2><p>{itemDescription(item)}</p><Link className="editorial-button" to={itemDestination(item)} target={item.type === 'video' ? '_blank' : undefined} rel={item.type === 'video' ? 'noopener noreferrer' : undefined}><i className={`fas ${item.type === 'video' ? 'fa-play' : 'fa-arrow-right'}`} aria-hidden="true" />{item.type === 'collection' ? 'Esplora la serie' : item.type === 'article' ? "Leggi l'articolo" : 'Guarda su YouTube'}</Link></div>
    </article>
  );
}

export function ContentCard({ item }: { item: EditorialItem }) {
  const destination = itemDestination(item);
  const date = itemDate(item);
  const isExternal = item.type === 'video';
  return (
    <article className={`editorial-card editorial-card--${item.type}`}>
      <Link to={destination} className="editorial-card__main" target={isExternal ? '_blank' : undefined} rel={isExternal ? 'noopener noreferrer' : undefined}>
        <div className="editorial-card__media"><img src={itemImage(item)} alt={item.type === 'article' ? item.article.coverAlt : itemTitle(item)} loading="lazy" decoding="async" />{item.type === 'collection' && <span className="episode-count"><i className="fas fa-layer-group" aria-hidden="true" />{item.match.videoRefs?.length} episodi</span>}</div>
        <div className="editorial-card__body"><ContentBadge type={item.type} /><h3>{itemTitle(item)}</h3><p>{itemDescription(item)}</p>{item.type === 'collection' && <span className="editorial-button editorial-card__cta">Esplora la serie <i className="fas fa-arrow-right" aria-hidden="true" /></span>}</div>
      </Link>
      <div className="editorial-card__footer">{date && <time dateTime={date}><i className="far fa-calendar-alt" aria-hidden="true" />{new Date(date).toLocaleDateString('it-IT', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' })}</time>}<ShareMenu url={item.type === 'video' ? `https://youtu.be/${item.match.contentId}` : `https://morwalpiz.com${destination}`} title={itemTitle(item)} /></div>
    </article>
  );
}

function ShareMenu({ url, title }: { url: string; title: string }) {
  const [open, setOpen] = useState(false);
  const menu = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!open) return;
    function dismiss(event: PointerEvent) {
      if (!menu.current?.contains(event.target as Node)) setOpen(false);
    }
    document.addEventListener('pointerdown', dismiss);
    return () => document.removeEventListener('pointerdown', dismiss);
  }, [open]);
  return <div ref={menu} className="share-menu" onBlur={(event) => { if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setOpen(false); }} onKeyDown={(event) => { if (event.key === 'Escape') { setOpen(false); menu.current?.querySelector('button')?.focus(); } }}><button type="button" className="icon-button" aria-label={`Condividi: ${title}`} title="Condividi" aria-expanded={open} onClick={() => setOpen(!open)}><i className="fas fa-share" aria-hidden="true" /></button>{open && <div className="share-menu__options" role="group" aria-label="Condividi su"><FacebookShareButton url={url} aria-label="Condividi su Facebook"><FacebookIcon size={28} round /></FacebookShareButton><WhatsappShareButton url={url} title={title} separator=":: " aria-label="Condividi su WhatsApp"><WhatsappIcon size={28} round /></WhatsappShareButton></div>}</div>;
}

function DonationCard() {
  return <section className="donation-panel" aria-labelledby="donation-heading"><i className="fas fa-mug-hot" aria-hidden="true" /><div><h2 id="donation-heading">Sostieni il canale</h2><p>Aiutami nel mio percorso!</p></div><a className="editorial-button" href="https://www.buymeacoffee.com/MorWalPiz" target="_blank" rel="noopener noreferrer">Offrimi un caffè <i className="fas fa-arrow-right" aria-hidden="true" /></a></section>;
}

function ShortsCard() {
  return <article className="shorts-card"><a href="https://www.youtube.com/@morwalpiz/shorts" target="_blank" rel="noopener noreferrer"><img src="https://morwalpizblob.blob.core.windows.net/page-images/home/stories.jpg" alt="Shorts MorWalPiz" loading="lazy" decoding="async" /><span className="content-badge content-badge--shorts"><i className="fas fa-bolt" aria-hidden="true" />Shorts</span><i className="fas fa-play shorts-card__play" aria-hidden="true" /><div><h3>Poco tempo?<br />Ci sono gli Shorts.</h3><span>Guarda su YouTube <i className="fas fa-arrow-right" aria-hidden="true" /></span></div></a></article>;
}

function ChannelNewsBanner({ item }: { item: ChannelNews }) {
  return <Link to={`/channel-news/${item.id}`} className="channel-news-banner"><img src={item.channelLogoUrl || '/images/logo-150.png'} alt={item.channelName} loading="lazy" decoding="async" /><span><small>{item.channelName}</small><strong>{item.title}</strong>{item.subtitle && <span>{item.subtitle}</span>}</span><i className="fas fa-arrow-right" aria-hidden="true" /></Link>;
}

function Sponsors({ sponsors }: { sponsors: SponsorItem[] }) {
  const [activeIndex, setActiveIndex] = useState(0);
  if (!sponsors.length) return <div className="home-sponsors home-sponsors--empty"><Link to="/sponsors">I miei sponsor <i className="fas fa-arrow-right" aria-hidden="true" /></Link></div>;
  const visible = Array.from({ length: Math.min(3, sponsors.length) }, (_, offset) => sponsors[(activeIndex + offset) % sponsors.length]);
  return (
    <section className="home-sponsors" aria-roledescription="carousel" aria-label="I miei sponsor">
      <div className="home-sponsors__heading"><h2><Link to="/sponsors">I miei sponsor</Link></h2><p>Al mio fianco, sul campo.</p></div>
      {sponsors.length > 2 && <button type="button" className="icon-button sponsor-control" onClick={() => setActiveIndex((activeIndex + sponsors.length - 1) % sponsors.length)} aria-label="Sponsor precedente"><i className="fas fa-chevron-left" aria-hidden="true" /></button>}
      <div className="home-sponsors__slides" aria-live="polite">{visible.map((sponsor) => <a key={sponsor.id} href={sponsor.url} target="_blank" rel="noopener noreferrer" title={sponsor.title} className="home-sponsors__link"><img src={sponsor.imgSrc} alt={sponsor.title} loading="lazy" decoding="async" /></a>)}</div>
      {sponsors.length > 2 && <button type="button" className="icon-button sponsor-control" onClick={() => setActiveIndex((activeIndex + 1) % sponsors.length)} aria-label="Sponsor successivo"><i className="fas fa-chevron-right" aria-hidden="true" /></button>}
    </section>
  );
}