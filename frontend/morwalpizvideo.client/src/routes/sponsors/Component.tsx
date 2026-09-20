import { useState, useEffect, useCallback } from 'react';
import { Link, useLoaderData } from 'react-router';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { useFetcher } from 'react-router';
import { CustomFormRenderer } from '@morwalpiz/layout';
import type { AnyAnswer, CustomForm, OpenAnswer } from '@morwalpizvideo/models';
import { askForSponsor } from '@services/sponsors';
import './style.scss';

interface SponsorItem {
  id: string;
  title: string;
  imgSrc: string;
  url: string;
  channelId?: string;
  shortLinkId?: string;
}

export function LegacySponsors() {
  const { sponsors } = useLoaderData() as { sponsors: SponsorItem[] };
  const fetcher = useFetcher();
  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
    (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  const { executeRecaptcha } = useGoogleReCaptcha();
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [description, setDescription] = useState('');
  const [token, setToken] = useState('');

  const handleReCaptchaVerify = useCallback(async () => {
    if (!executeRecaptcha) {
      return;
    }
    const token = await executeRecaptcha('sponsorForm');
    setToken(token);
  }, [executeRecaptcha]);

  useEffect(() => {
    handleReCaptchaVerify();
  }, [handleReCaptchaVerify]);
  return (
    <>
      <h1 className="text-center mb-3">SPONSORS</h1>
      <div className="row text-center mb-5">
        {sponsors.map((sponsor: SponsorItem) => (
          <div key={sponsor.title} className="col-12 col-sm-6 col-md-4 position-relative">
            <img className="mw-100" src={sponsor.imgSrc} />
            <Link
              to={sponsor.url}
              target="_blank"
              rel="noopener noreferrer"
              className="stretched-link"
            ></Link>
          </div>
        ))}
      </div>
      <div className="row">
        <div className="col-12 offset-md-8 col-md-4 bg-light p-3">
          <h4 className="mb-0">Vuoi aiutarmi anche tu in questa avventura?</h4>
          <p className="fs-08 mb-1">
            Lascia alcune informazioni base e verrai ricontattato, grazie!
          </p>
          <fetcher.Form className="w-[30%] mx-auto flex flex-col items-center gap-5" method="post">
            <input type="hidden" name="token" value={token} />
            <div className="form-group">
              <label className="form-label fw-bold">Nome</label>
              <input
                type="text"
                name="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="form-control"
                placeholder="Nome"
              />
              {errors?.email ? <em className="text-danger">{errors.name}</em> : null}
            </div>
            <div className="form-group">
              <label className="form-label fw-bold">Email</label>
              <input
                type="mail"
                name="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="form-control"
                placeholder="Email"
              />
              {errors?.email ? <em className="text-danger">{errors.email}</em> : null}
            </div>
            <div className="form-group">
              <label className="form-label fw-bold">Descrizione</label>
              <textarea
                className="form-control"
                rows={5}
                name="description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Breve descrizione"
              ></textarea>
              {errors?.description ? <em className="text-danger">{errors.description}</em> : null}
            </div>
            <div className="form-group fs-08 mt-1">
              Sito protetto da reCAPTCHA con Google
              {/*This site is protected by reCAPTCHA and the Google*/}
              &nbsp;<a href="https://policies.google.com/privacy">Privacy Policy</a> and{' '}
              <a href="https://policies.google.com/terms">Terms of Service</a>.
            </div>
            <div className="d-flex justify-content-between">
              {result ? <p className="text-success m-0 mt-1">{result.title}</p> : <p></p>}
              <div className="text-end">
                <button type="submit" className="btn btn-secondary">
                  {busy ? 'Saving...' : 'Save'}
                </button>
              </div>
            </div>
          </fetcher.Form>
        </div>
      </div>
    </>
  );
}

function getOpenText(answer: AnyAnswer | undefined): string {
  return answer && 'textResponse' in answer ? (answer as OpenAnswer).textResponse : '';
}

export default function Sponsors() {
  const { sponsors, form } = useLoaderData() as { sponsors: SponsorItem[]; form?: CustomForm };
  const { executeRecaptcha } = useGoogleReCaptcha();

  return (
    <>
      <h1 className="text-center mb-3">SPONSORS</h1>
      <section className="card sponsors-card mb-5" aria-labelledby="sponsors-list-title">
        <div className="card-body">
          <h2 id="sponsors-list-title" className="visually-hidden">
            Sponsor list
          </h2>
          <div className="sponsors-card__grid">
            {sponsors.map((sponsor) => (
              <article key={sponsor.title} className="sponsors-card__item position-relative">
                <div className="sponsors-card__image-frame">
                  <img className="sponsors-card__image" src={sponsor.imgSrc} alt={sponsor.title} />
                </div>
                <p className="sponsors-card__title">{sponsor.title}</p>
                <Link
                  to={sponsor.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="stretched-link"
                  aria-label={sponsor.title}
                ></Link>
              </article>
            ))}
          </div>
        </div>
      </section>
      {form && (
        <div className="row">
          <div className="col-12 offset-md-8 col-md-4 bg-light p-3">
            <CustomFormRenderer
              form={form}
              getRecaptchaToken={async () =>
                executeRecaptcha ? executeRecaptcha('sponsorForm') : null
              }
              onSubmit={async (answers, recaptchaToken) => {
                const [name, email, description] = answers.map(getOpenText);
                await askForSponsor(name, email, description, recaptchaToken ?? '');
              }}
            />
          </div>
        </div>
      )}
    </>
  );
}
