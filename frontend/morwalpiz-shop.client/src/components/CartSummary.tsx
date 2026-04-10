import { Link } from 'react-router';
import type { Cart } from '@morwalpizvideo/models';

interface CartSummaryProps {
  cart: Cart | null;
  onRemoveItem?: (productId: string) => void;
  loading?: boolean;
}

export default function CartSummary({ cart, onRemoveItem, loading = false }: CartSummaryProps) {
  const calculateTotal = () => {
    if (!cart || !cart.items) return 0;
    return cart.items.reduce((sum, item) => sum + (item.price || 0) * item.quantity, 0);
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('it-IT', {
      style: 'currency',
      currency: 'EUR',
    }).format(price);
  };

  if (loading) {
    return (
      <div className="text-center py-3">
        <div className="spinner-border spinner-border-sm" role="status">
          <span className="visually-hidden">Caricamento...</span>
        </div>
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="card">
        <div className="card-body text-center">
          <p className="mb-3">Il tuo carrello è vuoto</p>
          <Link to="/catalog" className="btn btn-primary">
            Vai al Catalogo
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="card">
      <div className="card-header">
        <h5 className="mb-0">Riepilogo Carrello</h5>
      </div>
      <ul className="list-group list-group-flush">
        {cart.items.map((item) => (
          <li key={item.productId} className="list-group-item">
            <div className="d-flex justify-content-between align-items-start">
              <div className="flex-grow-1">
                <h6 className="mb-1">{item.productName}</h6>
                <small className="text-muted">Quantità: {item.quantity}</small>
              </div>
              <div className="text-end">
                <div className="fw-bold">
                  {formatPrice((item.price || 0) * item.quantity)}
                </div>
                {onRemoveItem && (
                  <button
                    className="btn btn-sm btn-link text-danger p-0 mt-1"
                    onClick={() => onRemoveItem(item.productId)}
                  >
                    Rimuovi
                  </button>
                )}
              </div>
            </div>
          </li>
        ))}
      </ul>
      <div className="card-footer">
        <div className="d-flex justify-content-between align-items-center mb-3">
          <strong>Totale:</strong>
          <strong className="h5 mb-0 text-primary">{formatPrice(calculateTotal())}</strong>
        </div>
        <Link to="/checkout" className="btn btn-success w-100">
          Procedi al Checkout
        </Link>
      </div>
    </div>
  );
}