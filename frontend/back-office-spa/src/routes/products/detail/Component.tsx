import React from 'react';
import { useLoaderData, Link } from 'react-router';
import { Card, Badge } from 'react-bootstrap';
import type { Product } from '@morwalpizvideo/models';

const ProductDetail: React.FC = () => {
  const { product } = useLoaderData() as { product: Product };

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h1>Product Details</h1>
        <div>
          <Link to={`/products/${product.id}/edit`} className="btn btn-primary me-2">
            Edit
          </Link>
          <Link to="/products" className="btn btn-secondary">
            Back to List
          </Link>
        </div>
      </div>

      <Card>
        <Card.Body>
          <div className="mb-3">
            <strong>Title:</strong>
            <p>{product.title}</p>
          </div>

          <div className="mb-3">
            <strong>Description:</strong>
            <p>{product.description}</p>
          </div>

          <div className="mb-3">
            <strong>URL:</strong>
            <p>
              <a href={product.url} target="_blank" rel="noopener noreferrer">
                {product.url}
              </a>
            </p>
          </div>

          <div className="mb-3">
            <strong>Categories:</strong>
            <div className="mt-2">
              {product.categories && product.categories.length > 0 ? (
                product.categories.map((cat) => (
                  <Badge key={cat.id} bg="secondary" className="me-1">
                    {cat.title}
                  </Badge>
                ))
              ) : (
                <p className="text-muted">No categories</p>
              )}
            </div>
          </div>

          <div className="mb-3">
            <strong>Created:</strong>
            <p>{new Date(product.creationDateTime).toLocaleString()}</p>
          </div>
        </Card.Body>
      </Card>
    </div>
  );
};

export default ProductDetail;
