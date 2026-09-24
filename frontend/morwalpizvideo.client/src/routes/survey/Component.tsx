import { CustomFormRenderer } from '@morwalpiz/layout';
import type { Survey } from '@morwalpizvideo/models';
import { useLoaderData } from 'react-router';
import { submitFormResponse } from '../../services/customForms';
import { useState } from 'react';

export default function SurveyPage() {
  const { survey } = useLoaderData() as { survey: Survey };
  const forms = survey.forms ?? [];
  const [currentIndex, setCurrentIndex] = useState(0);
  const [completed, setCompleted] = useState(false);
  const currentForm = forms[currentIndex];

  if (completed) {
    return (
      <main className="container my-4">
        <h1>{survey.title}</h1>
        <div className="alert alert-success" role="status">
          Grazie, hai completato il sondaggio.
        </div>
      </main>
    );
  }

  if (!currentForm) {
    return (
      <main className="container my-4">
        <div className="alert alert-warning">Il sondaggio non contiene moduli disponibili.</div>
      </main>
    );
  }

  return (
    <main className="container my-4">
      <header className="mb-4">
        <h1>{survey.title}</h1>
        {survey.description && <p className="text-muted">{survey.description}</p>}
      </header>
      <section aria-labelledby={`survey-form-${currentForm.id}`}>
        <p className="text-muted">
          Modulo {currentIndex + 1} di {forms.length}
        </p>
        <h2 id={`survey-form-${currentForm.id}`} className="h4">
          {currentForm.title}
        </h2>
        <CustomFormRenderer
          form={currentForm}
          getRecaptchaToken={async () => null}
          onSubmit={async (answers) => {
            await submitFormResponse(currentForm.id, answers, survey.id);
            if (currentIndex === forms.length - 1) setCompleted(true);
            else setCurrentIndex((index) => index + 1);
          }}
        />
      </section>
    </main>
  );
}
