import { Helmet } from 'react-helmet-async';

export default function Index() {
  return (
    <>
      <Helmet>
        <title>MorWalPiz Shop - Documenti Digitali</title>
        <meta name="description" content="Shop online per documenti digitali di qualità" />
      </Helmet>

      <div className="hero-section">
        <div className="container text-center">
          <h1 className="display-4 mb-3">Benvenuto su MorWalPiz Shop</h1>
          <p className="lead mb-4">Documenti Digitali di Qualità</p>
          <a href="/catalog" className="btn btn-light btn-lg">
            Esplora il Catalogo
          </a>
        </div>
      </div>

      <div className="container my-5">
        <div className="row">
          <div className="col-md-4 mb-4">
            <div className="card h-100 text-center p-4">
              <div className="card-body">
                <h3 className="card-title">
                  <i className="bi bi-file-earmark-check fs-1 text-primary"></i>
                </h3>
                <h5 className="card-title">Qualità Garantita</h5>
                <p className="card-text">Documenti digitali verificati e di alta qualità</p>
              </div>
            </div>
          </div>

          <div className="col-md-4 mb-4">
            <div className="card h-100 text-center p-4">
              <div className="card-body">
                <h3 className="card-title">
                  <i className="bi bi-download fs-1 text-success"></i>
                </h3>
                <h5 className="card-title">Download Immediato</h5>
                <p className="card-text">Accesso istantaneo ai tuoi acquisti</p>
              </div>
            </div>
          </div>

          <div className="col-md-4 mb-4">
            <div className="card h-100 text-center p-4">
              <div className="card-body">
                <h3 className="card-title">
                  <i className="bi bi-shield-check fs-1 text-info"></i>
                </h3>
                <h5 className="card-title">Sicuro e Protetto</h5>
                <p className="card-text">Pagamenti sicuri e dati protetti</p>
              </div>
            </div>
          </div>
        </div>

        <div className="text-center mt-5">
          <h2>Inizia a Esplorare</h2>
          <p className="text-muted mb-4">
            Scopri la nostra selezione di documenti digitali
          </p>
          <a href="/catalog" className="btn btn-primary btn-lg">
            Vai al Catalogo
          </a>
        </div>
      </div>
    </>
  );
}