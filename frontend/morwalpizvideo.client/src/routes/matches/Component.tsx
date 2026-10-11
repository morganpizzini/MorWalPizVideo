import { useEffect, useState } from 'react';
import { useLoaderData } from 'react-router';
import './style.scss';
import SEO from '@utils/seo';
import DateDisplay from '@utils/date-display';
import ReactGA from 'react-ga4';
import { VideoPlayer } from '@morwalpiz/layout';
interface MatchVideoRef {
  youtubeId: string;
  title: string;
  description?: string;
  category?: string;
  publishedAt: string;
}
interface MatchData {
  title: string;
  description?: string;
  thumbnailUrl: string;
  videoRefs: MatchVideoRef[];
}
interface MatchImage {
  source: string;
  title: string;
  description: string;
}

export default function Matches() {
  const { match, images } = useLoaderData() as { match: MatchData; images: MatchImage[] };
  if (typeof window !== 'undefined') {
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: match.title });
  }
  return (
    <>
      <SEO
        title={match.title}
        description={match.description ?? ''}
        imageUrl={`https://img.youtube.com/vi/${match.thumbnailUrl}/hqdefault.jpg`}
        type="article"
      />
      <h1>{match.title}</h1>
      {images.length > 0 && (
        <section aria-labelledby="match-images-heading" className="mb-4">
          <h2 id="match-images-heading" className="h4">
            Images
          </h2>
          <GalleryComponent images={images} />
        </section>
      )}
      <div id="video-container" className="row">
        {match.videoRefs.map((video: MatchVideoRef) => (
          <div key={video.youtubeId} className="col-12 mb-3">
            <div key={video.youtubeId} className="video-block border rounded border-dark bg-white">
              <div className="row">
                <div className="col-md-6">
                  <div className="video-thumbnail">
                    <VideoPlayer youtubeId={video.youtubeId} height="306px" />
                  </div>
                </div>
                <div className="col-md-6 d-flex align-items-center">
                  <div className="video-details">
                    <p className="text-muted">{video.category}</p>
                    <div className="video-title">{video.title}</div>
                    <div className="video-description">{video.description}</div>
                    <DateDisplay className="text-muted text-end" dateString={video.publishedAt} />
                  </div>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>
    </>
  );
}

function GalleryComponent({ images }: { images: MatchImage[] }) {
  const [activeIndex, setActiveIndex] = useState(0);

  useEffect(() => {
    setActiveIndex(0);
  }, [images]);

  useEffect(() => {
    if (images.length < 2) return;
    const timer = window.setInterval(() => {
      setActiveIndex((currentIndex) => (currentIndex + 1) % images.length);
    }, 5000);
    return () => window.clearInterval(timer);
  }, [images.length]);

  if (images.length === 0) return null;
  const image = images[activeIndex];

  return (
    <div className="match-image-carousel" aria-roledescription="carousel" aria-label="Match images">
      <img
        src={image.source}
        alt={image.title}
        className="img-fluid d-block mx-auto"
        style={{ maxHeight: '70vh', objectFit: 'contain' }}
      />
      {images.length > 1 && (
        <div className="d-flex justify-content-between align-items-center mt-2">
          <button
            type="button"
            className="btn btn-outline-secondary"
            aria-label="Previous image"
            onClick={() => setActiveIndex((activeIndex - 1 + images.length) % images.length)}
          >
            <i className="fa fa-chevron-left" aria-hidden="true" />
          </button>
          <span aria-live="polite">
            {activeIndex + 1} / {images.length}
          </span>
          <button
            type="button"
            className="btn btn-outline-secondary"
            aria-label="Next image"
            onClick={() => setActiveIndex((activeIndex + 1) % images.length)}
          >
            <i className="fa fa-chevron-right" aria-hidden="true" />
          </button>
        </div>
      )}
    </div>
  );
}
