import { useResolvedLoaderData } from '@/router/asyncData';
import React, { useRef, useState } from 'react';
import { Alert, Button, ProgressBar } from 'react-bootstrap';
import { useRevalidator } from 'react-router';
import { deleteGalleryImage, uploadGalleryImages } from '@morwalpizvideo/services';
import type { Gallery } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';
import DetailPanel from '@components/DetailPanel';
import { PageSkeleton } from '@components/LoadingSkeleton';

export default function GalleryDetail(): React.ReactElement {
  const rawGallery = useResolvedLoaderData<Gallery | { data?: Gallery } | undefined>();
  const gallery: Gallery | undefined =
    rawGallery && 'id' in rawGallery ? rawGallery : rawGallery?.data;
  const revalidator = useRevalidator();
  const inputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');
  const [uploadStatuses, setUploadStatuses] = useState<Record<string, 'pending' | 'done' | 'failed'>>({});

  if (!gallery) return <PageSkeleton />;
  const galleryId = gallery.id;

  async function upload(event: React.ChangeEvent<HTMLInputElement>) {
    const files = Array.from(event.target.files ?? []);
    if (!files.length) return;
    setUploading(true);
    setError('');
    const initial = Object.fromEntries(files.map(file => [file.name, 'pending' as const]));
    setUploadStatuses(initial);
    for (const file of files) {
      try {
        await uploadGalleryImages(galleryId, [file]);
        setUploadStatuses(current => ({ ...current, [file.name]: 'done' }));
      } catch {
        setUploadStatuses(current => ({ ...current, [file.name]: 'failed' }));
      }
    }
    revalidator.revalidate();
    if (inputRef.current) inputRef.current.value = '';
    setUploading(false);
  }

  async function removeImage(index: number) {
    if (!window.confirm('Remove this image?')) return;
    try {
      await deleteGalleryImage(galleryId, index);
      revalidator.revalidate();
    } catch {
      setError('Unable to remove the image.');
    }
  }

  return (
    <>
      <PageHeader
        title={gallery.name}
        backLink="/galleries"
        editLink={`/galleries/${gallery.id}/edit`}
      />
      <DetailPanel title="Gallery Details">
        <p>
          <strong>Slug:</strong> /{gallery.slug}
        </p>
        <p>
          <strong>Description:</strong> {gallery.description || 'No description'}
        </p>
        <p>
          <strong>Downloads:</strong> {gallery.downloadCount}
        </p>
        <Button as="label" variant="outline-primary" disabled={uploading}>
          {uploading ? 'Uploading...' : 'Upload images'}
          <input
            ref={inputRef}
            className="d-none"
            type="file"
            accept="image/*"
            multiple
            onChange={upload}
          />
        </Button>
        {error && (
          <div className="text-danger mt-2" role="alert">
            {error}
          </div>
        )}
        {Object.keys(uploadStatuses).length > 0 && (
          <Alert variant="secondary" className="mt-3" aria-live="polite">
            {Object.entries(uploadStatuses).map(([name, status]) => (
              <div key={name} className="d-flex justify-content-between gap-2">
                <span className="text-break">{name}</span>
                <span>{status === 'pending' ? 'Processing...' : status === 'done' ? 'Complete' : 'Failed'}</span>
              </div>
            ))}
            {uploading && <ProgressBar className="mt-2" animated now={100} />}
          </Alert>
        )}
      </DetailPanel>
      <div className="row g-3 mt-1">
        {gallery.images.length ? (
          gallery.images.map(image => (
            <div className="col-6 col-md-3" key={image.storageKey}>
              <figure className="border rounded p-2 h-100 mb-0">
                <img className="img-fluid" src={image.previewPublicUrl ?? image.publicUrl} alt="" />
                <figcaption className="text-muted small mt-2">
                  {image.width} x {image.height}
                </figcaption>
                <Button className="mt-2" size="sm" variant="outline-danger" onClick={() => removeImage(gallery.images.indexOf(image))}>
                  Remove
                </Button>
              </figure>
            </div>
          ))
        ) : (
          <div className="col-12 text-muted">No images uploaded.</div>
        )}
      </div>
    </>
  );
}
