/**
 * Shopping Cart Page Component
 * 
 * Displays the user's shopping cart with items and checkout option
 */

import { useLoaderData, useNavigate, useNavigation } from 'react-router';
import { Helmet } from 'react-helmet-async';
import { useState, useEffect } from 'react';
import { getShopCart, updateCartItem, removeFromCart, checkoutCart } from '@morwalpizvideo/services';
import type { Cart } from '@morwalpizvideo/models';
import { useAuth } from '../contexts/AuthContext';

// Skeleton loader for cart items
function CartItemSkeleton() {
  return (
    <div className="row mb-4 pb-4 border-bottom">
      <div className="col-md-2">
        <div className="bg-light rounded" style={{ height: '80px' }}>
          <div className="placeholder-glow h-100 d-flex align-items-center justify-content-center">
            <span className="placeholder col-6"></span>
          </div>
        </div>
      </div>
      <div className="col-md-6">
        <h5 className="mb-1 placeholder-glow">
          <span className="placeholder col-8"></span>
        </h5>
        <p className="text-muted small mb-0 placeholder-glow">
          <span className="placeholder col-6"></span>
        </p>
      </div>
      <div className="col-md-4">
        <div className="d-flex flex-column align-items-end">
          <p className="mb-2 placeholder-glow">
            <span className="placeholder col-4"></span>
          </p>
          <div className="mb-2 placeholder-glow">
            <span className="placeholder col-6"></span>
          </div>
          <button className="btn btn-link btn-sm text-danger p-0 disabled" aria-disabled="true">
            <span className="placeholder col-4"></span>
          </button>
        </div>
      </div>
    </div>
  );
}

// Skeleton loader for full cart page
function CartPageSkeleton() {
  return (
    <>
      <Helmet>
        <title>Carrello - MorWalPiz Shop</title>
      </Helmet>

      <div className="container my-5">
        <h1 className="mb-4">Il Tuo Carrello</h1>

        <div className="row">
          <div className="col-lg-8">
            <div className="card mb-4">
              <div className="card-body">
                <CartItemSkeleton />
                <CartItemSkeleton />
                <CartItemSkeleton />
              </div>
            </div>
          </div>

          <div className="col-lg-4">
            <div className="card">
              <div className="card-body">
                <h5 className="card-title mb-4">Riepilogo Ordine</h5>
                <div className="placeholder-glow">
                  <p className="placeholder col-12"></p>
                  <p className="placeholder col-10"></p>
                  <div className="d-grid mt-4">
                    <button className="btn btn-primary btn-lg disabled" aria-disabled="true">
                      <span className="placeholder col-6"></span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

export async function loader(): Promise<Cart> {
  try {
    const cart = await getShopCart();
    return cart;
  } catch (error) {
    console.error('Error loading cart:', error);
    // Return empty cart if error
    return {
      id: '',
      customerId: '',
      items: [],
      isCompleted: false,
      createdAt: new Date(),
      updatedAt: new Date(),
    };
  }
}

export default function CartPage() {
  const initialCart = useLoaderData() as Cart;
  const [cart, setCart] = useState(initialCart);
  const navigate = useNavigate();
  const navigation = useNavigation();
  const { isAuthenticated, session } = useAuth();
  const [isProcessing, setIsProcessing] = useState(false);
  const [isUpdatingItem, setIsUpdatingItem] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const isLoading = navigation.state === 'loading';

  // Update cart when loader data changes
  useEffect(() => {
    setCart(initialCart);
  }, [initialCart]);

  // Clear error when loading
  useEffect(() => {
    if (isLoading) {
      setError(null);
    }
  }, [isLoading]);

  if (isLoading) {
    return <CartPageSkeleton />;
  }

  // Calculate total amount from items
  const totalAmount = cart.items.reduce((sum, item) => {
    return sum + (item.price || 0) * item.quantity;
  }, 0);

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('it-IT', {
      style: 'currency',
      currency: 'EUR',
    }).format(price);
  };

  const handleUpdateQuantity = async (productId: string, quantity: number) => {
    if (quantity < 1) return;

    setIsUpdatingItem(productId);
    setError(null);

    try {
      const updatedCart = await updateCartItem({ productId, quantity });
      setCart(updatedCart);
    } catch (err: any) {
      console.error('Error updating cart:', err);
      setError('Errore durante l\'aggiornamento. Riprova.');
    } finally {
      setIsUpdatingItem(null);
    }
  };

  const handleRemoveItem = async (productId: string) => {
    setIsUpdatingItem(productId);
    setError(null);

    try {
      const updatedCart = await removeFromCart(productId);
      setCart(updatedCart);
    } catch (err: any) {
      console.error('Error removing item:', err);
      setError('Errore durante la rimozione. Riprova.');
    } finally {
      setIsUpdatingItem(null);
    }
  };

  const handleCheckout = async () => {
    if (!isAuthenticated) {
      navigate('/login?redirect=/cart');
      return;
    }

    if (!session?.email) {
      setError('Sessione non valida. Effettua nuovamente il login.');
      return;
    }

    setIsProcessing(true);
    setError(null);

    try {
      const result = await checkoutCart({ 
        cartId: cart.id,
        email: session.email
      });
      navigate(`/checkout-success?orderId=${result.orderId}`);
    } catch (err: any) {
      console.error('Error during checkout:', err);
      setError('Errore durante il checkout. Riprova.');
    } finally {
      setIsProcessing(false);
    }
  };

  if (!cart.items || cart.items.length === 0) {
    return (
      <>
        <Helmet>
          <title>Carrello - MorWalPiz Shop</title>
        </Helmet>

        <div className="container my-5">
          <h1 className="mb-4">Il Tuo Carrello</h1>

          <div className="alert alert-info">
            <h4 className="alert-heading">Carrello vuoto</h4>
            <p className="mb-0">
              Non hai ancora aggiunto prodotti al carrello.{' '}
              <a href="/catalog" className="alert-link">
                Vai al catalogo
              </a>{' '}
              per iniziare lo shopping!
            </p>
          </div>
        </div>
      </>
    );
  }

  return (
    <>
      <Helmet>
        <title>Carrello ({cart.items.length}) - MorWalPiz Shop</title>
      </Helmet>

      <div className="container my-5">
        <h1 className="mb-4">Il Tuo Carrello</h1>

        {error && (
          <div className="alert alert-danger alert-dismissible fade show" role="alert">
            {error}
            <button
              type="button"
              className="btn-close"
              onClick={() => setError(null)}
              aria-label="Close"
            ></button>
          </div>
        )}

        <div className="row">
          <div className="col-lg-8">
            <div className="card mb-4">
              <div className="card-body">
                {cart.items.map((item) => {
                  const isItemUpdating = isUpdatingItem === item.productId;
                  return (
                    <div key={item.productId} className="row mb-4 pb-4 border-bottom">
                      <div className="col-md-2">
                        <div className="bg-light rounded d-flex align-items-center justify-content-center" style={{ height: '80px' }}>
                          {isItemUpdating ? (
                            <span className="spinner-border spinner-border-sm text-muted" role="status" aria-hidden="true"></span>
                          ) : (
                            <i className="bi bi-file-earmark-text fs-3 text-muted"></i>
                          )}
                        </div>
                      </div>
                      <div className="col-md-6">
                        <h5 className="mb-1">{item.productName}</h5>
                        <p className="text-muted small mb-0">
                          Documento digitale
                        </p>
                      </div>
                      <div className="col-md-4">
                        <div className="d-flex flex-column align-items-end">
                          <p className="mb-2">
                            <strong>{formatPrice(item.price || 0)}</strong>
                          </p>
                          <div className="input-group mb-2" style={{ maxWidth: '120px' }}>
                            <button
                              className="btn btn-outline-secondary btn-sm"
                              type="button"
                              onClick={() =>
                                handleUpdateQuantity(item.productId, item.quantity - 1)
                              }
                              disabled={isItemUpdating || item.quantity <= 1}
                            >
                              -
                            </button>
                            <input
                              type="text"
                              className="form-control form-control-sm text-center"
                              value={item.quantity}
                              readOnly
                            />
                            <button
                              className="btn btn-outline-secondary btn-sm"
                              type="button"
                              onClick={() =>
                                handleUpdateQuantity(item.productId, item.quantity + 1)
                              }
                              disabled={isItemUpdating}
                            >
                              +
                            </button>
                          </div>
                          <button
                            className="btn btn-link btn-sm text-danger p-0"
                            onClick={() => handleRemoveItem(item.productId)}
                            disabled={isItemUpdating}
                          >
                            {isItemUpdating ? (
                              <>
                                <span className="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
                                Rimozione...
                              </>
                            ) : (
                              'Rimuovi'
                            )}
                          </button>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>

            <a href="/catalog" className="btn btn-outline-secondary">
              <i className="bi bi-arrow-left me-2"></i>
              Continua lo Shopping
            </a>
          </div>

          <div className="col-lg-4">
            <div className="card">
              <div className="card-body">
                <h5 className="card-title mb-4">Riepilogo Ordine</h5>

                <div className="d-flex justify-content-between mb-2">
                  <span>Subtotale</span>
                  <span>{formatPrice(totalAmount)}</span>
                </div>
                <div className="d-flex justify-content-between mb-3">
                  <span>IVA (22%)</span>
                  <span>{formatPrice(totalAmount * 0.22)}</span>
                </div>

                <hr />

                <div className="d-flex justify-content-between mb-4">
                  <strong>Totale</strong>
                  <strong className="text-primary fs-4">
                    {formatPrice(totalAmount * 1.22)}
                  </strong>
                </div>

                <div className="d-grid">
                  <button
                    className="btn btn-primary btn-lg"
                    onClick={handleCheckout}
                    disabled={isProcessing || cart.items.length === 0}
                  >
                    {isProcessing ? (
                      <>
                        <span
                          className="spinner-border spinner-border-sm me-2"
                          role="status"
                          aria-hidden="true"
                        ></span>
                        Elaborazione...
                      </>
                    ) : (
                      'Procedi al Checkout'
                    )}
                  </button>
                </div>

                <p className="text-muted small mt-3 mb-0">
                  <i className="bi bi-shield-check me-1"></i>
                  Pagamento sicuro e protetto
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}