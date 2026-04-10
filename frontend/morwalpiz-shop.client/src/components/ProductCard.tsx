import { Link } from 'react-router';
import type { DigitalProduct } from '@morwalpizvideo/models';

interface ProductCardProps {
  product: DigitalProduct;
}

export default function ProductCard({ product }: ProductCardProps) {
  const formatPrice = (price?: number) => {
    if (!price) return 'Gratis';
    return new Intl.NumberFormat('it-IT', {
      style: 'currency',
      currency: 'EUR',
    }).format(price);
  };

  return (
    <div className="card h-100">
      <img
        src={product.previewImageUrl || '/images/placeholder.png'}
        className="card-img-top"
        alt={product.name}
        style={{ height: '200px', objectFit: 'cover' }}
      />
      <div className="card-body d-flex flex-column">
        <h5 className="card-title">{product.name}</h5>
        <p className="card-text flex-grow-1">
          {product.description.length > 100
            ? `${product.description.substring(0, 100)}...`
            : product.description}
        </p>
        <div className="d-flex justify-content-between align-items-center mt-3">
          <span className="h5 mb-0 text-primary">{formatPrice(product.price)}</span>
          <Link
            to={`/products/${product.id}`}
            className="btn btn-primary btn-sm"
          >
            Dettagli
          </Link>
        </div>
      </div>
    </div>
  );
}