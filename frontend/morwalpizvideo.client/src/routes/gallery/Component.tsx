import { useEffect, useState, type JSX } from 'react';
import { useLoaderData } from 'react-router';
import type { GalleryPublic } from '@morwalpizvideo/models';
import SEO from '@utils/seo';
import { downloadGallery } from '../../services/gallery';

type ViewMode = 'grid' | 'list';

function getFilename(imageUrl: string, index: number): string {
  try {
    const filename = decodeURIComponent(new URL(imageUrl).pathname.split('/').pop() ?? '');
    return filename || `image-${index + 1}`;
  } catch {
    return `image-${index + 1}`;
  }
}

function saveBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

export default function Gallery(): JSX.Element {
  const gallery = useLoaderData() as GalleryPublic;
  const [viewMode, setViewMode] = useState<ViewMode>('grid');
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [activeIndex, setActiveIndex] = useState<number | null>(null);
  const [isDownloading, setIsDownloading] = useState(false);
  const [downloadingIndex, setDownloadingIndex] = useState<number | null>(null);

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

  const allSelected =
    gallery.images.length > 0 && selected.size === gallery.images.length;

  const handleDownload = async () => {
    setIsDownloading(true);
    try {
      const indexes = selected.size ? [...selected].sort((a, b) => a - b) : undefined;
      const blob = await downloadGallery(gallery.slug, indexes);
      const filename =
        indexes?.length === 1
          ? getFilename(gallery.images[indexes[0]], indexes[0])
          : `${gallery.slug}.zip`;
      saveBlob(blob, filename);
    } finally {
      setIsDownloading(false);
    }
  };

  const handleImageDownload = async (index: number) => {
    setDownloadingIndex(index);
    try {
      const blob = await downloadGallery(gallery.slug, [index]);
      saveBlob(blob, getFilename(gallery.images[index], index));
    } finally {
      setDownloadingIndex(null);
    }
  };

  return (
    <main className="container gallery-page py-4 py-lg-5">
      <SEO title={gallery.name} description={gallery.description ?? ''} type="article" />
      <div className="gallery-header d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">
        <div>
          <p className="gallery-kicker mb-1">Gallery</p>
          <h1 className="mb-0">{gallery.name}</h1>
        </div>
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
      <div className="gallery-toolbar d-flex flex-wrap align-items-center justify-content-between gap-2 py-3 px-3 mb-4">
        <div className="d-flex flex-wrap align-items-center gap-3">
          <span className="gallery-selection" aria-live="polite">
            {selected.size ? `${selected.size} selected` : `${gallery.images.length} photos`}
          </span>
          <div className="btn-group" role="group" aria-label="Gallery selection">
            <button
              type="button"
              className="btn btn-outline-secondary"
              disabled={gallery.images.length === 0 || allSelected}
              onClick={() => setSelected(new Set(gallery.images.map((_, index) => index)))}
            >
              Select all
            </button>
            <button
              type="button"
              className="btn btn-outline-secondary"
              disabled={selected.size === 0}
              onClick={() => setSelected(new Set())}
            >
              Deselect all
            </button>
          </div>
        </div>
        <button
          type="button"
          className="btn btn-primary"
          disabled={isDownloading || gallery.images.length === 0}
          onClick={handleDownload}
        >
          {isDownloading ? 'Downloading...' : selected.size ? 'Download selected' : 'Download all'}
        </button>
      </div>
      {viewMode === 'grid' ? (
        <div className="gallery-grid row g-3">
          {gallery.images.map((image, index) => (
            <article
              className="gallery-card col-12 col-sm-6 col-lg-4 position-relative"
              key={`${image}-${index}`}
            >
              <label className="gallery-checkbox form-check position-absolute m-2 rounded">
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
                className="gallery-image-button border-0 bg-transparent p-0 w-100"
                onClick={() => setActiveIndex(index)}
                aria-label={`Open image ${index + 1}`}
              >
                <img
                  className="gallery-image img-fluid w-100"
                  src={image}
                  alt={`${gallery.name} ${index + 1}`}
                  loading="lazy"
                />
              </button>
            </article>
          ))}
        </div>
      ) : (
        <div className="gallery-table-wrap table-responsive">
          <table className="gallery-table table align-middle mb-0">
            <thead>
              <tr>
                <th scope="col">Filename</th>
                <th scope="col" className="text-end">Download</th>
              </tr>
            </thead>
            <tbody>
              {gallery.images.map((image, index) => (
                <tr key={`${image}-${index}`}>
                  <td>
                    <div className="d-flex align-items-center gap-3">
                      <label className="gallery-checkbox gallery-checkbox--table form-check rounded">
                        <input
                          className="form-check-input"
                          type="checkbox"
                          checked={selected.has(index)}
                          onChange={() => toggleSelected(index)}
                          aria-label={`Select ${getFilename(image, index)}`}
                        />
                      </label>
                      <button
                        type="button"
                        className="gallery-image-button gallery-table-image border-0 bg-transparent p-0"
                        onClick={() => setActiveIndex(index)}
                        aria-label={`Open image ${index + 1}`}
                      >
                        <img
                          className="gallery-image gallery-list-image img-fluid"
                          src={image}
                          alt={`${gallery.name} ${index + 1}`}
                          loading="lazy"
                        />
                      </button>
                      <span className="text-break">{getFilename(image, index)}</span>
                    </div>
                  </td>
                  <td className="text-end">
                    <button
                      type="button"
                      className="btn btn-outline-primary"
                      onClick={() => handleImageDownload(index)}
                      disabled={isDownloading || downloadingIndex !== null}
                      aria-label={`Download ${getFilename(image, index)}`}
                    >
                      {downloadingIndex === index ? 'Downloading...' : 'Download'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {activeIndex !== null && (
        <div
          className="gallery-modal modal d-block"
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
                  {getFilename(gallery.images[activeIndex], activeIndex)}
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
                  className="gallery-modal-image img-fluid"
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
