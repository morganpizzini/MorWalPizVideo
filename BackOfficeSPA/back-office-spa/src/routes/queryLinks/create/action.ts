import { data } from 'react-router';

import { CreateQueryLinkDTO } from '@/models';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData()) as CreateQueryLinkDTO;
  const errors: Record<string, string | string[]> = {}; // Configura errors come un dizionario

  // Validazione dei campi
  if (!values.title || values.title.trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.description || values.description.trim().length === 0) {
    errors['description'] = 'Description cannot be empty';
  }

  // Verifica se ci sono errori
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // Se non ci sono errori, esegui la richiesta API
  return fetch(`/api/querylinks`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values),
  })
    .then(() => {
      return data({ success: true }, { status: 201 });
    })
    .catch(() => {
      errors['generics'] = ['API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}
