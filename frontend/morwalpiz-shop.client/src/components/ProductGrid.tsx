import type { DigitalProduct } from '@morwalpizvideo/models';
import ProductCard from './ProductCard';

interface ProductGridProps {
  products: DigitalProduct[];
  loading?: boolean;
}

export default function ProductGrid({ products, loading = false }: ProductGridProps) {
  if (loading) {
    return (
      <div className="text-center py-5">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Caricamento...</span>
        </div>
      </div>
    );
  }

  if (products.length === 0) {
    return (
      <div className="alert alert-info text-center" role="alert">
        Nessun prodotto disponibile al momento.
      </div>
    );
  }

  return (
    <div className="row g-4">
      {products.map((product) => (
        <div key={product.id} className="col-12 col-md-6 col-lg-4">
          <ProductCard product={product} />
        </div>
      ))}
    </div>
  );
}