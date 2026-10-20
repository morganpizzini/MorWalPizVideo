import { useState } from 'react';
import type { JSX } from 'react';
import { useLoaderData, useRevalidator } from 'react-router';
import type { Gallery, SaveGalleryRequest } from '@morwalpizvideo/models';
import {
  createGallery,
  deleteGallery,
  updateGallery,
  uploadGalleryImages,
} from '@morwalpizvideo/services';

const emptyForm: SaveGalleryRequest = { name: '', description: '', slug: '', expirationDays: 7 };

export default function Galleries(): JSX.Element {
  const rawGalleries = useLoaderData() as Gallery[] | { data?: Gallery[] } | undefined;
  const galleries = Array.isArray(rawGalleries)
    ? rawGalleries
    : Array.isArray(rawGalleries?.data)
      ? rawGalleries.data
      : [];
  const revalidator = useRevalidator();
  const [form, setForm] = useState<SaveGalleryRequest>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const selected = galleries.find(item => item.id === selectedId);

  async function save(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (editingId) await updateGallery(editingId, form);
    else await createGallery(form);
    setForm(emptyForm);
    setEditingId(null);
    revalidator.revalidate();
  }

  function edit(item: Gallery) {
    setEditingId(item.id);
    setForm({
      name: item.name,
      description: item.description ?? '',
      slug: item.slug,
      expirationDays: 7,
    });
    setSelectedId(item.id);
  }

  async function upload(event: React.ChangeEvent<HTMLInputElement>) {
    if (selectedId && event.target.files?.length) {
      await uploadGalleryImages(selectedId, Array.from(event.target.files));
      revalidator.revalidate();
    }
  }

  async function remove(item: Gallery) {
    if (window.confirm(`Delete ${item.name}?`)) {
      await deleteGallery(item.id);
      if (selectedId === item.id) setSelectedId(null);
      revalidator.revalidate();
    }
  }

  return (
    <main className="container py-4">
      <h1>Galleries</h1>
      <form onSubmit={save} className="row g-3 mb-4">
        <div className="col-md-4">
          <label className="form-label">
            Name
            <input
              className="form-control"
              required
              value={form.name}
              onChange={event => setForm({ ...form, name: event.target.value })}
            />
          </label>
        </div>
        <div className="col-md-4">
          <label className="form-label">
            Slug
            <input
              className="form-control"
              required
              value={form.slug}
              onChange={event => setForm({ ...form, slug: event.target.value })}
            />
          </label>
        </div>
        <div className="col-md-4">
          <label className="form-label">
            Expiration
            <select
              className="form-select"
              value={form.expirationDays}
              onChange={event =>
                setForm({ ...form, expirationDays: Number(event.target.value) as 3 | 7 | 21 })
              }
            >
              <option value={3}>3 days</option>
              <option value={7}>7 days</option>
              <option value={21}>21 days</option>
            </select>
          </label>
        </div>
        <div className="col-12">
          <label className="form-label">
            Description
            <textarea
              className="form-control"
              value={form.description}
              onChange={event => setForm({ ...form, description: event.target.value })}
            />
          </label>
        </div>
        <div className="col-12">
          <button className="btn btn-primary" type="submit">
            {editingId ? 'Save changes' : 'Create gallery'}
          </button>
          {editingId && (
            <button
              className="btn btn-link"
              type="button"
              onClick={() => {
                setEditingId(null);
                setForm(emptyForm);
              }}
            >
              Cancel
            </button>
          )}
        </div>
      </form>
      <div className="list-group">
        {galleries.map(item => (
          <div
            className="list-group-item d-flex justify-content-between align-items-center"
            key={item.id}
          >
            <button
              className="btn btn-link text-start"
              type="button"
              onClick={() => setSelectedId(item.id)}
            >
              <strong>{item.name}</strong>
              <span className="d-block text-muted">
                /{item.slug} | {item.downloadCount} downloads | {item.images.length} images
              </span>
            </button>
            <span>
              <button
                className="btn btn-sm btn-outline-secondary me-2"
                type="button"
                onClick={() => edit(item)}
              >
                Edit
              </button>
              <button
                className="btn btn-sm btn-outline-danger"
                type="button"
                onClick={() => remove(item)}
              >
                Delete
              </button>
            </span>
          </div>
        ))}
      </div>
      {selected && (
        <section className="mt-4">
          <h2>{selected.name}</h2>
          <p>{selected.description}</p>
          <p>Downloads: {selected.downloadCount}</p>
          <label className="btn btn-outline-primary">
            Upload images
            <input className="d-none" type="file" accept="image/*" multiple onChange={upload} />
          </label>
          <div className="row mt-3">
            {selected.images.map(image => (
              <div className="col-6 col-md-3" key={image.storageKey}>
                <img className="img-fluid" src={image.publicUrl} alt="" />
              </div>
            ))}
          </div>
        </section>
      )}
    </main>
  );
}
