import { useRouteError, isRouteErrorResponse } from 'react-router';
import { Helmet } from 'react-helmet-async';

export default function ErrorPage() {
  const error = useRouteError();

  let errorMessage: string;
  let errorStatus: number | undefined;

  if (isRouteErrorResponse(error)) {
    errorMessage = error.statusText || error.data?.message || 'An error occurred';
    errorStatus = error.status;
  } else if (error instanceof Error) {
    errorMessage = error.message;
  } else {
    errorMessage = 'Unknown error';
  }

  return (
    <>
      <Helmet>
        <title>Errore - MorWalPiz Shop</title>
      </Helmet>

      <div className="container my-5">
        <div className="row justify-content-center">
          <div className="col-md-8 text-center">
            <h1 className="display-1 text-danger">
              {errorStatus || '!'}
            </h1>
            <h2 className="mb-4">Oops! Qualcosa è andato storto</h2>
            <p className="lead text-muted mb-4">{errorMessage}</p>
            <a href="/" className="btn btn-primary btn-lg">
              Torna alla Home
            </a>
          </div>
        </div>
      </div>
    </>
  );
}