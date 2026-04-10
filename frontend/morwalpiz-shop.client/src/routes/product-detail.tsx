/**
 * Product Detail Page Component
 * 
 * Displays detailed information about a single product
 */

import { useLoaderData, useNavigate, LoaderFunctionArgs, useNavigation } from 'react-router';
import { Helmet } from 'react-helmet-async';
import { useState, useEffect } from 'react';
import { getShopProduct, addToCart } from '@morwalpizvideo/services';
import type { DigitalProduct } from '@morwalpizvideo/models';
import { useAuth } from '../contexts/AuthContext';

// Skeleton loader component
function ProductDetailSkeleton() {
  return (
    <div className="container my-5">
      <nav aria-label="breadcrumb">
        <ol className="breadcrumb placeholder-glow">
          <li className="breadcrumb-item">
            <span className="placeholder col-2"></span>
          </li>
          <li className="breadcrumb-item">
            <span className="placeholder col-3"></span>
          </li>
          <li className="breadcrumb-item">
            <span className="placeholder col-4"></span>
          </li>
        </ol>
      </nav>

      <div className="row">
        <div className="col-lg-6">
          <div
            className="bg-light rounded shadow"
            style={{ height: '400px' }}
          >
            <div className="placeholder-glow h-100 d-flex align-items-center justify-content-center">
              <span className="placeholder col-6"></span>
            </div>
          </div>
        </div>

        <div className="col-lg-6">
          <h1 className="mb-3 placeholder-glow">
            <span className="placeholder col-8"></span>
          </h1>

          <div className="mb-4 placeholder-glow">
            <span className="placeholder col-4"></span>
          </div>

          <div className="mb-4 placeholder-glow">
            <span className="placeholder col-3"></span>
          </div>

          <div className="mb-4">
            <h5 className="placeholder-glow">
              <span className="placeholder col-3"></span>
            </h5>
            <p className="placeholder-glow">
              <span className="placeholder col-12"></span>
              <span className="placeholder col-10"></span>
              <span className="placeholder col-11"></span>
            </p>
          </div>

          <div className="d-grid gap-2 d-md-flex">
            <button className="btn btn-primary btn-lg disabled" aria-disabled="true">
              <span className="placeholder col-6"></span>
            </button>
            <button className="btn btn-outline-primary btn-lg disabled" aria-disabled="true">
              <span className="placeholder col-4"></span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

export async function loader({ params }: LoaderFunctionArgs): Promise<DigitalProduct> {
  const { productId } = params;
  
  if (!productId) {
    throw new Response('Product ID is required', { status: 400 });
  }

  try {
    const product = await getShopProduct(productId);
    return product;
  } catch (error) {
    console.error('Error loading product:', error);
    throw new Response('Product not found', { status: 404 });
  }
}

export default function ProductDetail() {
  const product = useLoaderData() as DigitalProduct;
  const navigate = useNavigate();
  const navigation = useNavigation();
  const { isAuthenticated } = useAuth();
  const [isAdding, setIsAdding] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const isLoading = navigation.state === 'loading';

  // Clear messages when loading new product
  useEffect(() => {
    if (isLoading) {
      setError(null);
      setSuccess(false);
    }
  }, [isLoading]);

  if (isLoading) {
    return <ProductDetailSkeleton />;
  }

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('it-IT', {
      style: 'currency',
      currency: 'EUR',
    }).format(price);
  };

  const formatDate = (date: Date) => {
    return new Date(date).toLocaleDateString('it-IT', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  const handleAddToCart = async () => {
    if (!isAuthenticated) {
      navigate(`/login?redirect=/products/${product.id}`);
      return;
    }

    setIsAdding(true);
    setError(null);
    setSuccess(false);

    try {
      await addToCart({
        productId: product.id,
        quantity: 1,
      });
      setSuccess(true);
      setTimeout(() => setSuccess(false), 3000);
    } catch (err: any) {
      console.error('Error adding to cart:', err);
      setError('Errore durante l\'aggiunta al carrello. Riprova.');
    } finally {
      setIsAdding(false);
    }
  };

  return (
    <>
      <Helmet>
        <title>{product.name} - MorWalPiz Shop</title>
        <meta name="description" content={product.description} />
      </Helmet>

      <div className="container my-5">
        <nav aria-label="breadcrumb">
          <ol className="breadcrumb">
            <li className="breadcrumb-item">
              <a href="/">Home</a>
            </li>
            <li className="breadcrumb-item">
              <a href="/catalog">Catalogo</a>
            </li>
            <li className="breadcrumb-item active" aria-current="page">
              {product.name}
            </li>
          </ol>
        </nav>

        <div className="row">
          <div className="col-lg-6">
            {product.previewImageUrl ? (
              <img
                src={product.previewImageUrl}
                alt={product.name}
                className="img-fluid rounded shadow"
              />
            ) : (
              <div
                className="bg-light d-flex align-items-center justify-content-center rounded"
                style={{ height: '400px' }}
              >
                <span className="text-muted">Nessuna anteprima disponibile</span>
              </div>
            )}
          </div>

          <div className="col-lg-6">
            <h1 className="mb-3">{product.name}</h1>

            <div className="mb-4">
              <span className="badge bg-success fs-6 me-2">
                {product.isActive ? 'Disponibile' : 'Non disponibile'}
              </span>
            </div>

            <div className="mb-4">
              <h3 className="text-primary">{formatPrice(product.price || 0)}</h3>
            </div>

            <div className="mb-4">
              <h5>Descrizione</h5>
              <p className="text-muted">{product.description}</p>
            </div>

            {error && (
              <div className="alert alert-danger" role="alert">
                {error}
              </div>
            )}

            {success && (
              <div className="alert alert-success" role="alert">
                Prodotto aggiunto al carrello con successo!{' '}
                <a href="/cart" className="alert-link">
                  Vai al carrello
                </a>
              </div>
            )}

            {product.isActive && (
              <div className="d-grid gap-2 d-md-flex">
                <button
                  className="btn btn-primary btn-lg"
                  onClick={handleAddToCart}
                  disabled={isAdding}
                >
                  {isAdding ? (
                    <>
                      <span
                        className="spinner-border spinner-border-sm me-2"
                        role="status"
                        aria-hidden="true"
                      ></span>
                      Aggiunta in corso...
                    </>
                  ) : (
                    <>
                      <i className="bi bi-cart-plus me-2"></i>
                      Aggiungi al Carrello
                    </>
                  )}
                </button>
                <a href="/cart" className="btn btn-outline-primary btn-lg">
                  <i className="bi bi-cart me-2"></i>
                  Vai al Carrello
                </a>
              </div>
            )}

            {!product.isActive && (
              <div className="alert alert-warning">
                Questo prodotto non è attualmente disponibile per l'acquisto.
              </div>
            )}

            <hr className="my-4" />

            <div className="small text-muted">
              <p className="mb-1">
                <strong>Creato il:</strong> {formatDate(product.createdAt)}
              </p>
              <p className="mb-0">
                <strong>Ultimo aggiornamento:</strong> {formatDate(product.updatedAt)}
              </p>
            </div>
          </div>
        </div>

        <div className="mt-5">
          <a href="/catalog" className="btn btn-outline-secondary">
            <i className="bi bi-arrow-left me-2"></i>
            Torna al Catalogo
          </a>
        </div>
      </div>
    </>
  );
}