import { useLoaderData } from 'react-router';
import SEO from '@utils/seo';
import ReactGA from 'react-ga4';
import { CustomFormRenderer } from '@morwalpiz/layout';
import type { AnyAnswer, CustomForm, PagePublic } from '@morwalpizvideo/models';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { submitFormResponse } from '../../services/customForms';
export default function Matches() {
  const { page, form } = useLoaderData() as { page: PagePublic; form: CustomForm | null };
  const { executeRecaptcha } = useGoogleReCaptcha();
  const description = page.content.replace(/<[^>]+>/g, '').slice(0, 120);
  if (typeof window !== 'undefined') {
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: page.title });
  }
  return (
    <>
      <SEO
        title={page.title}
        description={description}
        imageUrl={page.thumbnailUrl}
        type="article"
      />
      <div id="page-container" className="p-4 bg-white">
        <h1 className="page-title">{page.title}</h1>
        <hr />
        {page.videoId && (
          <iframe
            width="100%"
            height="450px"
            className="rounded"
            src={`https://www.youtube.com/embed/${page.videoId}?autoplay=1&mute=1`}
            title="YouTube video player"
            frameBorder="0"
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
            referrerPolicy="strict-origin-when-cross-origin"
            allowFullScreen
          ></iframe>
        )}
        <div className="page-text row align-items-center">
          {page.thumbnailUrl.length > 0 && (
            <div
              className={`text-center col-12 ${page.content.length > 0 ? 'col-md-3' : 'col-md-4 offset-md-4'} order-1 order-md-2`}
            >
              <img className="img-fluid" alt={page.title} src={page.thumbnailUrl} />
            </div>
          )}
          {page.content.length > 0 && (
            <div
              className="col-12 col-md-9 order-2 order-md-1 p-3"
              dangerouslySetInnerHTML={{ __html: page.content }}
            ></div>
          )}
        </div>
        {form && (
          <CustomFormRenderer
            form={form}
            getRecaptchaToken={async () =>
              executeRecaptcha ? executeRecaptcha('customForm') : null
            }
            onSubmit={async (answers: AnyAnswer[]) => {
              await submitFormResponse(form.id, answers);
            }}
          />
        )}
      </div>
    </>
  );
}
