/**
 * Checkout Success Page Component
 * 
 * Displays order confirmation after successful checkout
 */

import { useSearchParams, Link } from 'react-router';
import { Helmet } from 'react-helmet-async';

export default function CheckoutSuccessPage() {
  const [searchParams] = useSearchParams();
  const orderId = searchParams.get('orderId');

  return (
    <>
      <Helmet>
        <title>Ordine Completato - MorWalPiz Shop</title>
      </Helmet>

      <div className="container my-5">
        <div className="row justify-content-center">
          <div className="col-lg-8">
            <div className="card shadow-sm">
              <div className="card-body text-center py-5">
                <div className="mb-4">
                  <i className="bi bi-check-circle-fill text-success" style={{ fontSize: '5rem' }}></i>
                </div>

                <h1 className="mb-3">Ordine Completato!</h1>
                
                <p className="lead text-muted mb-4">
                  Grazie per il tuo acquisto. Il tuo ordine è stato elaborato con successo.
                </p>

                {orderId && (
                  <div className="alert alert-info mb-4">
                    <strong>Numero Ordine:</strong> {orderId}
                  </div>
                )}

                <div className="bg-light rounded p-4 mb-4">
                  <h5 className="mb-3">
                    <i className="bi bi-envelope-fill me-2"></i>
                    Conferma via Email
                  </h5>
                  <p className="mb-0">
                    Ti abbiamo inviato una email di conferma con i dettagli dell'ordine e
                    le istruzioni per accedere ai tuoi contenuti digitali.
                  </p>
                </div>

                <div className="bg-light rounded p-4 mb-4">
                  <h5 className="mb-3">
                    <i className="bi bi-download me-2"></i>
                    Accesso ai Contenuti
                  </h5>
                  <p className="mb-0">
                    I tuoi documenti digitali sono ora disponibili nel tuo account.
                    Riceverai un link di download nella email di conferma.
                  </p>
                </div>

                <div className="d-grid gap-2 d-md-block mt-4">
                  <Link to="/catalog" className="btn btn-primary btn-lg me-md-2">
                    <i className="bi bi-arrow-left me-2"></i>
                    Torna al Catalogo
                  </Link>
                  <Link to="/" className="btn btn-outline-secondary btn-lg">
                    <i className="bi bi-house-fill me-2"></i>
                    Vai alla Home
                  </Link>
                </div>

                <hr className="my-4" />

                <div className="text-muted">
                  <h6 className="mb-3">Hai bisogno di aiuto?</h6>
                  <p className="mb-2">
                    <i className="bi bi-telegram me-2"></i>
                    Contattaci su Telegram:{' '}
                    <a href="https://t.me/morwalpiz" target="_blank" rel="noopener noreferrer">
                      @morwalpiz
                    </a>
                  </p>
                  <p className="mb-0">
                    <i className="bi bi-envelope me-2"></i>
                    Email:{' '}
                    <a href="mailto:support@morwalpizshop.it">support@morwalpizshop.it</a>
                  </p>
                </div>
              </div>
            </div>

            <div className="mt-4 p-4 bg-light rounded">
              <h5 className="mb-3">
                <i className="bi bi-info-circle-fill me-2"></i>
                Prossimi Passi
              </h5>
              <ol className="mb-0">
                <li className="mb-2">
                  Controlla la tua casella email (anche nello spam) per la conferma d'ordine
                </li>
                <li className="mb-2">
                  Segui le istruzioni nella email per accedere ai tuoi contenuti digitali
                </li>
                <li className="mb-2">
                  Salva i link di download in un luogo sicuro per accessi futuri
                </li>
                <li className="mb-0">
                  In caso di problemi, contattaci tramite i canali di supporto indicati sopra
                </li>
              </ol>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}