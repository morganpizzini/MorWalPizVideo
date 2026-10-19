import { useEffect, useState, type JSX } from 'react';
import { useLoaderData } from 'react-router';
import type { GalleryPublic } from '@morwalpizvideo/models';
import SEO from '@utils/seo';
import { downloadGallery } from '../../services/gallery';

type ViewMode = 'grid' | 'list';

export default function Gallery(): JSX.Element {
  const gallery = useLoaderData() as GalleryPublic;
  const [viewMode, setViewMode] = useState<ViewMode>('grid');
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [activeIndex, setActiveIndex] = useState<number | null>(null);
  const [isDownloading, setIsDownloading] = useState(false);

  useEffect(() => {
    if (activeIndex === null) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setActiveIndex(null);
      if (event.key === 'ArrowLeft')
        setActiveIndex((index) =>
          index === null ? null : (index - 1 + gallery.images.length) % gallery.images.length
        );
      if (event.key === 'ArrowRight')
        setActiveIndex((index) => (index === null ? null : (index + 1) % gallery.images.length));
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [activeIndex, gallery.images.length]);

  const toggleSelected = (index: number) =>
    setSelected((current) => {
      const next = new Set(current);
      if (next.has(index)) next.delete(index);
      else next.add(index);
      return next;
    });

  const handleDownload = async () => {
    setIsDownloading(true);
    try {
      const blob = await downloadGallery(
        gallery.slug,
        selected.size ? [...selected].sort((a, b) => a - b) : undefined
      );
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download =
        selected.size === 1 ? `${gallery.slug}-${[...selected][0] + 1}` : `${gallery.slug}.zip`;
      link.click();
      URL.revokeObjectURL(url);
    } finally {
      setIsDownloading(false);
    }
  };

  return (
    <main className="container py-4">
      <SEO title={gallery.name} description={gallery.description ?? ''} type="article" />
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-2">
        <h1 className="mb-0">{gallery.name}</h1>
        <div className="btn-group" role="group" aria-label="Gallery view">
          <button
            type="button"
            className={`btn btn-outline-secondary ${viewMode === 'grid' ? 'active' : ''}`}
            aria-pressed={viewMode === 'grid'}
            onClick={() => setViewMode('grid')}
          >
            Grid
          </button>
          <button
            type="button"
            className={`btn btn-outline-secondary ${viewMode === 'list' ? 'active' : ''}`}
            aria-pressed={viewMode === 'list'}
            onClick={() => setViewMode('list')}
          >
            List
          </button>
        </div>
      </div>
      {gallery.description && <p className="mb-4">{gallery.description}</p>}
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 border-top border-bottom py-2 mb-3">
        <span aria-live="polite">{selected.size} selected</span>
        <button
          type="button"
          className="btn btn-primary"
          disabled={isDownloading || gallery.images.length === 0}
          onClick={handleDownload}
        >
          {isDownloading ? 'Downloading...' : selected.size ? 'Download selected' : 'Download all'}
        </button>
      </div>
      <div className={viewMode === 'grid' ? 'row g-3' : 'd-flex flex-column gap-3'}>
        {gallery.images.map((image, index) => (
          <article
            className={
              viewMode === 'grid'
                ? 'col-12 col-sm-6 col-lg-4 position-relative'
                : 'd-flex gap-3 align-items-center position-relative'
            }
            key={`${image}-${index}`}
          >
            <label className="form-check position-absolute m-2 bg-white rounded px-2 py-1">
              <input
                className="form-check-input"
                type="checkbox"
                checked={selected.has(index)}
                onChange={() => toggleSelected(index)}
                aria-label={`Select image ${index + 1}`}
              />
            </label>
            <button
              type="button"
              className="border-0 bg-transparent p-0 w-100"
              onClick={() => setActiveIndex(index)}
              aria-label={`Open image ${index + 1}`}
            >
              <img
                className={`img-fluid w-100 ${viewMode === 'list' ? 'gallery-list-image' : ''}`}
                src={image}
                alt={`${gallery.name} ${index + 1}`}
                loading="lazy"
              />
            </button>
          </article>
        ))}
      </div>
      {activeIndex !== null && (
        <div
          className="modal d-block"
          role="dialog"
          aria-modal="true"
          aria-label={`${gallery.name} image ${activeIndex + 1}`}
          onClick={() => setActiveIndex(null)}
        >
          <div
            className="modal-dialog modal-xl modal-dialog-centered"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="modal-content">
              <div className="modal-header">
                <span>
                  {activeIndex + 1} / {gallery.images.length}
                </span>
                <button
                  type="button"
                  className="btn-close"
                  aria-label="Close image"
                  onClick={() => setActiveIndex(null)}
                />
              </div>
              <div className="modal-body text-center">
                <img
                  className="img-fluid"
                  src={gallery.images[activeIndex]}
                  alt={`${gallery.name} ${activeIndex + 1}`}
                />
              </div>
              {gallery.images.length > 1 && (
                <div className="modal-footer justify-content-between">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={() =>
                      setActiveIndex(
                        (activeIndex - 1 + gallery.images.length) % gallery.images.length
                      )
                    }
                  >
                    Previous
                  </button>
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={() => setActiveIndex((activeIndex + 1) % gallery.images.length)}
                  >
                    Next
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </main>
  );
}
