import React, { useEffect, useRef, useState } from 'react';
import { Button, Form } from 'react-bootstrap';
import { useFetcher, useLoaderData, useNavigate, useParams } from 'react-router';
import { deleteChannelNewsImage, uploadChannelNewsImages } from '@morwalpizvideo/services';
import type { ChannelNewsAdmin, ChannelNewsImage } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import PageHeader from '@components/PageHeader';
import RichTextEditor from './RichTextEditor';

const statusOptions = [
  ['0', 'Draft'],
  ['1', 'Scheduled'],
  ['2', 'Published'],
  ['3', 'Archived'],
] as const;

export default function ChannelNewsForm(): React.ReactElement {
  const entity = useLoaderData() as ChannelNewsAdmin | null;
  const { id } = useParams();
  const navigate = useNavigate();
  const toast = useToast();
  const fetcher = useFetcher();
  const [title, setTitle] = useState('');
  const [subtitle, setSubtitle] = useState('');
  const [slug, setSlug] = useState('');
  const [descriptionHtml, setDescriptionHtml] = useState('');
  const [status, setStatus] = useState('0');
  const [publicationTimeUtc, setPublicationTimeUtc] = useState('');
  const [displayOrder, setDisplayOrder] = useState('0');
  const [images, setImages] = useState<ChannelNewsImage[]>([]);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [imageError, setImageError] = useState('');
  const [imageBusy, setImageBusy] = useState(false);
  const imageInputRef = useRef<HTMLInputElement>(null);
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;

  useEffect(() => {
    setTitle(entity?.title ?? '');
    setSubtitle(entity?.subtitle ?? '');
    setSlug(entity?.slug ?? '');
    setDescriptionHtml(entity?.descriptionHtml ?? '');
    setStatus(String(entity?.status ?? 0));
    setPublicationTimeUtc(entity?.publicationTimeUtc ? entity.publicationTimeUtc.slice(0, 16) : '');
    setDisplayOrder(String(entity?.displayOrder ?? 0));
    setImages(
      [...(entity?.images ?? [])].sort((left, right) => left.displayOrder - right.displayOrder)
    );
    setSelectedFiles([]);
    setImageError('');
  }, [entity]);

  useEffect(() => {
    if (busy || !fetcher.data?.success) return;
    toast.show(
      'Success',
      id ? 'ChannelNews updated successfully' : 'ChannelNews created successfully',
      { variant: 'success' }
    );
    navigate('/channelnews');
  }, [busy, fetcher.data, id, navigate, toast]);

  const handleFileSelection = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? []);
    const available = 10 - images.length;
    if (files.length > available) {
      setSelectedFiles([]);
      setImageError(`Select at most ${available} more image${available === 1 ? '' : 's'}.`);
      return;
    }
    setImageError('');
    setSelectedFiles(files);
  };

  const uploadSelectedFiles = async () => {
    if (!id || selectedFiles.length === 0) return;
    setImageBusy(true);
    setImageError('');
    try {
      const response = (await uploadChannelNewsImages(id, selectedFiles)) as ChannelNewsAdmin & {
        errors?: string[];
      };
      if (response.errors) {
        setImageError(response.errors.join(', '));
        return;
      }
      setImages(
        [...(response.images ?? [])].sort((left, right) => left.displayOrder - right.displayOrder)
      );
      setSelectedFiles([]);
      if (imageInputRef.current) imageInputRef.current.value = '';
      toast.show('Success', 'ChannelNews images uploaded successfully', { variant: 'success' });
    } catch {
      setImageError('Unable to upload ChannelNews images.');
    } finally {
      setImageBusy(false);
    }
  };

  const deleteImage = async (imageIndex: number) => {
    if (!id) return;
    setImageBusy(true);
    setImageError('');
    try {
      const response = (await deleteChannelNewsImage(id, imageIndex)) as ChannelNewsAdmin & {
        errors?: string[];
      };
      if (response.errors) {
        setImageError(response.errors.join(', '));
        return;
      }
      setImages(
        [...(response.images ?? [])].sort((left, right) => left.displayOrder - right.displayOrder)
      );
      toast.show('Success', 'ChannelNews image deleted successfully', { variant: 'success' });
    } catch {
      setImageError('Unable to delete the ChannelNews image.');
    } finally {
      setImageBusy(false);
    }
  };

  return (
    <>
      <PageHeader title={id ? 'Edit ChannelNews' : 'Create ChannelNews'} />
      <GenericErrorList errors={errors?.generics} />
      <fetcher.Form method="post">
        <Form.Group className="mb-3">
          <Form.Label>Title</Form.Label>
          <Form.Control
            name="title"
            value={title}
            onChange={event => setTitle(event.target.value)}
            required
          />
          <FieldError error={errors?.title} />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Subtitle</Form.Label>
          <Form.Control
            name="subtitle"
            value={subtitle}
            onChange={event => setSubtitle(event.target.value)}
          />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>Slug</Form.Label>
          <Form.Control name="slug" value={slug} onChange={event => setSlug(event.target.value)} />
        </Form.Group>
        <div className="row g-3 mb-3">
          <Form.Group className="col-md-4">
            <Form.Label>Status</Form.Label>
            <Form.Select
              name="status"
              value={status}
              onChange={event => setStatus(event.target.value)}
            >
              {statusOptions.map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </Form.Select>
            <FieldError error={errors?.status} />
          </Form.Group>
          <Form.Group className="col-md-4">
            <Form.Label>Publication time UTC</Form.Label>
            <Form.Control
              type="datetime-local"
              name="publicationTimeUtc"
              value={publicationTimeUtc}
              onChange={event => setPublicationTimeUtc(event.target.value)}
            />
            <FieldError error={errors?.publicationTimeUtc} />
          </Form.Group>
          <Form.Group className="col-md-4">
            <Form.Label>Display order</Form.Label>
            <Form.Control
              type="number"
              name="displayOrder"
              value={displayOrder}
              onChange={event => setDisplayOrder(event.target.value)}
            />
          </Form.Group>
        </div>
        <Form.Group className="mb-3">
          <Form.Label>WYSIWYG HTML body</Form.Label>
          <RichTextEditor
            value={descriptionHtml}
            onChange={setDescriptionHtml}
            disabled={busy || imageBusy}
          />
          <input type="hidden" name="descriptionHtml" value={descriptionHtml} />
          <FieldError error={errors?.descriptionHtml} />
        </Form.Group>
        <Form.Group className="mb-3">
          <Form.Label>ChannelNews images</Form.Label>
          <Form.Control
            ref={imageInputRef}
            type="file"
            name={id ? undefined : 'images'}
            accept="image/*"
            multiple
            onChange={handleFileSelection}
            disabled={busy || imageBusy || images.length >= 10}
          />
          <Form.Text>
            Select up to {10 - images.length} additional images. Images are resized by the server
            without upscaling.
          </Form.Text>
          {selectedFiles.length > 0 && (
            <div className="mt-2">Selected: {selectedFiles.map(file => file.name).join(', ')}</div>
          )}
          {id && (
            <Button
              type="button"
              className="mt-2"
              variant="outline-primary"
              onClick={uploadSelectedFiles}
              disabled={selectedFiles.length === 0 || busy || imageBusy}
            >
              Upload selected images
            </Button>
          )}
          <FieldError error={errors?.images} />
          {imageError && (
            <div className="text-danger mt-2" role="alert">
              {imageError}
            </div>
          )}
        </Form.Group>
        {images.length > 0 && (
          <div className="row g-3 mb-3" aria-label="ChannelNews image previews">
            {images.map((image, index) => (
              <div className="col-sm-6 col-lg-4" key={`${image.publicUrl}-${index}`}>
                <div className="border rounded p-2 h-100">
                  <img
                    src={image.publicUrl}
                    alt={image.altText || `ChannelNews image ${index + 1}`}
                    className="img-fluid"
                  />
                  <div className="small mt-2">
                    #{index + 1} {image.altText || 'Untitled'}
                    <br />
                    {image.width} x {image.height} · {image.contentType}
                  </div>
                  {id && (
                    <Button
                      type="button"
                      variant="outline-danger"
                      size="sm"
                      className="mt-2"
                      onClick={() => deleteImage(index)}
                      disabled={busy || imageBusy}
                    >
                      Delete image
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
        <div className="d-flex gap-2">
          <Button type="submit" disabled={busy || imageBusy}>
            {busy ? 'Saving...' : 'Save ChannelNews'}
          </Button>
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate('/channelnews')}
            disabled={busy || imageBusy}
          >
            Cancel
          </Button>
        </div>
      </fetcher.Form>
    </>
  );
}
