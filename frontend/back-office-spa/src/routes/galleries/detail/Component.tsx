import { useResolvedLoaderData } from '@/router/asyncData';
import React, { useRef, useState } from 'react';
import { Button } from 'react-bootstrap';
import { useRevalidator } from 'react-router';
import { uploadGalleryImages } from '@morwalpizvideo/services';
import type { Gallery } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';
import DetailPanel from '@components/DetailPanel';
import { PageSkeleton } from '@components/LoadingSkeleton';

export default function GalleryDetail(): React.ReactElement {
  const rawGallery = useResolvedLoaderData<Gallery | { data?: Gallery }>();
  const gallery = 'data' in rawGallery ? rawGallery.data : rawGallery;
  const revalidator = useRevalidator();
  const inputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');

  async function upload(event: React.ChangeEvent<HTMLInputElement>) {
    const files = Array.from(event.target.files ?? []);
    if (!files.length) return;
    setUploading(true);
    setError('');
    try {
      await uploadGalleryImages(gallery.id, files);
      revalidator.revalidate();
      if (inputRef.current) inputRef.current.value = '';
    } catch {
      setError('Unable to upload gallery images.');
    } finally {
      setUploading(false);
    }
  }

  if (!gallery) return <PageSkeleton />;

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
      </DetailPanel>
      <div className="row g-3 mt-1">
        {gallery.images.length ? (
          gallery.images.map(image => (
            <div className="col-6 col-md-3" key={image.storageKey}>
              <figure className="border rounded p-2 h-100 mb-0">
                <img className="img-fluid" src={image.publicUrl} alt="" />
                <figcaption className="text-muted small mt-2">
                  {image.width} x {image.height}
                </figcaption>
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
