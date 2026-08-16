import React, { useEffect, useRef, useState } from 'react';
import { Button, Form } from 'react-bootstrap';
import { useFetcher, useLoaderData, useNavigate, useParams } from 'react-router';
import { deletePageImage, uploadPageImages } from '@morwalpizvideo/services';
import type { PageAdmin, PageImage } from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import PageHeader from '@components/PageHeader';
import RichTextEditor from './RichTextEditor';

export default function PageForm(): React.ReactElement {
  const entity = useLoaderData() as PageAdmin | null;
  const { id } = useParams();
  const navigate = useNavigate();
  const toast = useToast();
  const fetcher = useFetcher();
  const [title, setTitle] = useState('');
  const [url, setUrl] = useState('');
  const [content, setContent] = useState('');
  const [thumbnailUrl, setThumbnailUrl] = useState('');
  const [videoId, setVideoId] = useState('');
  const [status, setStatus] = useState('0');
  const [images, setImages] = useState<PageImage[]>([]);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [imageError, setImageError] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);
  const busy = fetcher.state !== 'idle';

  useEffect(() => { setTitle(entity?.title ?? ''); setUrl(entity?.url ?? ''); setContent(entity?.content ?? ''); setThumbnailUrl(entity?.thumbnailUrl ?? ''); setVideoId(entity?.videoId ?? ''); setStatus(String(entity?.status ?? 0)); setImages(entity?.inlineImages ?? []); setSelectedFiles([]); }, [entity]);
  useEffect(() => { if (!busy && fetcher.data?.success) { toast.show('Success', id ? 'Page updated successfully' : 'Page created successfully', { variant: 'success' }); navigate('/pages'); } }, [busy, fetcher.data, id, navigate, toast]);

  const upload = async () => {
    if (!id || selectedFiles.length === 0) return;
    setImageError('');
    try {
      const response = await uploadPageImages(id, selectedFiles);
      if ('errors' in (response as object)) { setImageError('Unable to upload page images.'); return; }
      const next = response as PageImage[];
      setImages(next);
      const inserted = next.slice(images.length).map(image => `<p><img src="${image.publicUrl}" alt="${image.altText}" /></p>`).join('');
      setContent(current => current + inserted);
      setSelectedFiles([]);
      if (inputRef.current) inputRef.current.value = '';
    } catch { setImageError('Unable to upload page images.'); }
  };

  const removeImage = async (index: number) => {
    if (!id) return;
    try { setImages(await deletePageImage(id, index)); } catch { setImageError('Unable to delete the page image.'); }
  };

  return <>
    <PageHeader title={id ? 'Edit page' : 'Create page'} backLink="/pages" />
    <GenericErrorList errors={fetcher.data?.errors?.generics} />
    <fetcher.Form method="post">
      <Form.Group className="mb-3"><Form.Label>Title</Form.Label><Form.Control name="title" value={title} onChange={event => setTitle(event.target.value)} required /><FieldError error={fetcher.data?.errors?.title} /></Form.Group>
      <div className="row g-3"><Form.Group className="col-md-8 mb-3"><Form.Label>Public URL slug</Form.Label><Form.Control name="url" value={url} onChange={event => setUrl(event.target.value)} required /><Form.Text>Published pages are available at /pages/{url || 'slug'}.</Form.Text><FieldError error={fetcher.data?.errors?.url} /></Form.Group><Form.Group className="col-md-4 mb-3"><Form.Label htmlFor="page-status">Status</Form.Label><Form.Select id="page-status" name="status" value={status} onChange={event => setStatus(event.target.value)}><option value="0">Draft</option><option value="1">Published</option></Form.Select></Form.Group></div>
      <div className="row g-3"><Form.Group className="col-md-6 mb-3"><Form.Label>Thumbnail URL</Form.Label><Form.Control name="thumbnailUrl" value={thumbnailUrl} onChange={event => setThumbnailUrl(event.target.value)} /></Form.Group><Form.Group className="col-md-6 mb-3"><Form.Label>Video ID</Form.Label><Form.Control name="videoId" value={videoId} onChange={event => setVideoId(event.target.value)} /></Form.Group></div>
      <Form.Group className="mb-3"><Form.Label>Page columns and HTML body</Form.Label><RichTextEditor value={content} onChange={setContent} disabled={busy} /><input type="hidden" name="content" value={content} /><FieldError error={fetcher.data?.errors?.content} /></Form.Group>
      <Form.Group className="mb-3"><Form.Label>Inline images</Form.Label><Form.Control ref={inputRef} type="file" accept="image/*" multiple disabled={!id || busy} onChange={event => setSelectedFiles(Array.from((event.target as HTMLInputElement).files ?? []))} /><Form.Text>Save the page before uploading. Uploaded images can be inserted into the editor and are resized by the server without upscaling.</Form.Text>{selectedFiles.length > 0 && <div>Selected: {selectedFiles.map(file => file.name).join(', ')}</div>}<Button type="button" className="mt-2" variant="outline-primary" disabled={!id || selectedFiles.length === 0} onClick={upload}>Upload and insert</Button>{imageError && <div className="text-danger" role="alert">{imageError}</div>}{images.length > 0 && <div className="row g-2 mt-2">{images.map((image, index) => <div className="col-sm-4" key={`${image.publicUrl}-${index}`}><img src={image.publicUrl} alt={image.altText} className="img-fluid" /><div className="d-flex gap-2"><span>{image.width} x {image.height}</span><Button size="sm" variant="link" onClick={() => removeImage(index)}>Delete image</Button></div></div>)}</div>}</Form.Group>
      <Button type="submit" disabled={busy}>Save page</Button>
    </fetcher.Form>
  </>;
}