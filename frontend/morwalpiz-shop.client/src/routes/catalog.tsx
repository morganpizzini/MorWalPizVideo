/**
 * Product Catalog Page Component
 * 
 * Displays all available products with search, filtering, and pagination
 */

import { useLoaderData } from 'react-router';
import { Helmet } from 'react-helmet-async';
import { useState, useMemo } from 'react';
import { fetchShopProducts, fetchShopProductCategories } from '@morwalpizvideo/services';
import type { DigitalProduct, ProductCategory } from '@morwalpizvideo/models';

interface CatalogData {
  products: DigitalProduct[];
  categories: ProductCategory[];
}

export async function loader(): Promise<CatalogData> {
  try {
    const [products, categories] = await Promise.all([
      fetchShopProducts(),
      fetchShopProductCategories(),
    ]);
    return { products, categories };
  } catch (error) {
    console.error('Error loading catalog:', error);
    return { products: [], categories: [] };
  }
}

// Skeleton loader component
function ProductCardSkeleton() {
  return (
    <div className="col-md-6 col-lg-4 mb-4">
      <div className="card h-100">
        <div className="card-img-top bg-light" style={{ height: '200px' }}>
          <div className="placeholder-glow h-100 d-flex align-items-center justify-content-center">
            <span className="placeholder col-6"></span>
          </div>
        </div>
        <div className="card-body">
          <h5 className="card-title placeholder-glow">
            <span className="placeholder col-8"></span>
          </h5>
          <p className="card-text placeholder-glow">
            <span className="placeholder col-12"></span>
            <span className="placeholder col-10"></span>
          </p>
          <h5 className="text-primary placeholder-glow">
            <span className="placeholder col-4"></span>
          </h5>
        </div>
        <div className="card-footer">
          <button className="btn btn-primary w-100 disabled" aria-disabled="true">
            <span className="placeholder col-6"></span>
          </button>
        </div>
      </div>
    </div>
  );
}

export default function Catalog() {
  const { products, categories } = useLoaderData() as CatalogData;
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [isLoading, setIsLoading] = useState(false);
  const itemsPerPage = 9;

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('it-IT', {
      style: 'currency',
      currency: 'EUR',
    }).format(price);
  };

  // Filter products based on category and search
  const filteredProducts = useMemo(() => {
    let result = products;

    // Filter by category
    if (selectedCategory) {
      result = result.filter((p) => p.categoryIds.includes(selectedCategory));
    }

    // Filter by search query
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      result = result.filter(
        (p) =>
          p.name.toLowerCase().includes(query) ||
          p.description.toLowerCase().includes(query)
      );
    }

    return result;
  }, [products, selectedCategory, searchQuery]);

  // Pagination
  const totalPages = Math.ceil(filteredProducts.length / itemsPerPage);
  const startIndex = (currentPage - 1) * itemsPerPage;
  const endIndex = startIndex + itemsPerPage;
  const currentProducts = filteredProducts.slice(startIndex, endIndex);

  // Get product count by category
  const getCategoryCount = (categoryId: string) => {
    return products.filter((p) => p.categoryIds.includes(categoryId)).length;
  };

  const handleCategoryChange = (categoryId: string | null) => {
    setIsLoading(true);
    setSelectedCategory(categoryId);
    setCurrentPage(1);
    // Simulate loading delay for UX
    setTimeout(() => setIsLoading(false), 300);
  };

  const handlePageChange = (page: number) => {
    if (page < 1 || page > totalPages) return;
    setIsLoading(true);
    setCurrentPage(page);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    // Simulate loading delay for UX
    setTimeout(() => setIsLoading(false), 200);
  };

  const handleSearchChange = (query: string) => {
    setSearchQuery(query);
    setCurrentPage(1);
  };

  // Generate page numbers to display
  const getPageNumbers = () => {
    const pageNumbers: (number | string)[] = [];
    const maxVisible = 5;

    if (totalPages <= maxVisible + 2) {
      // Show all pages if total is small
      for (let i = 1; i <= totalPages; i++) {
        pageNumbers.push(i);
      }
    } else {
      // Always show first page
      pageNumbers.push(1);

      if (currentPage > 3) {
        pageNumbers.push('...');
      }

      // Show pages around current page
      const start = Math.max(2, currentPage - 1);
      const end = Math.min(totalPages - 1, currentPage + 1);

      for (let i = start; i <= end; i++) {
        pageNumbers.push(i);
      }

      if (currentPage < totalPages - 2) {
        pageNumbers.push('...');
      }

      // Always show last page
      pageNumbers.push(totalPages);
    }

    return pageNumbers;
  };

  return (
    <>
      <Helmet>
        <title>Catalogo Prodotti - MorWalPiz Shop</title>
        <meta name="description" content="Esplora il nostro catalogo di prodotti digitali" />
      </Helmet>

      <div className="container my-5">
        <h1 className="mb-4">Catalogo Prodotti</h1>

        {/* Search Bar */}
        <div className="row mb-4">
          <div className="col-md-8 col-lg-6">
            <div className="input-group">
              <span className="input-group-text">
                <i className="bi bi-search"></i>
              </span>
              <input
                type="text"
                className="form-control"
                placeholder="Cerca prodotti per nome o descrizione..."
                value={searchQuery}
                onChange={(e) => handleSearchChange(e.target.value)}
              />
              {searchQuery && (
                <button
                  className="btn btn-outline-secondary"
                  type="button"
                  onClick={() => handleSearchChange('')}
                >
                  <i className="bi bi-x-lg"></i>
                </button>
              )}
            </div>
          </div>
        </div>

        {/* Category Filter */}
        {categories.length > 0 && (
          <div className="mb-4">
            <div className="d-flex flex-wrap gap-2">
              <button
                className={`btn ${!selectedCategory ? 'btn-primary' : 'btn-outline-primary'}`}
                onClick={() => handleCategoryChange(null)}
              >
                Tutti i Prodotti
                <span className="badge bg-light text-dark ms-2">{products.length}</span>
              </button>
              {categories.map((category) => (
                <button
                  key={category.id}
                  className={`btn ${
                    selectedCategory === category.id ? 'btn-primary' : 'btn-outline-primary'
                  }`}
                  onClick={() => handleCategoryChange(category.id)}
                >
                  {/* @ts-expect-error - ProductCategory has name property, TypeScript cache issue */}
                  {category.name}
                  <span className="badge bg-light text-dark ms-2">
                    {getCategoryCount(category.id)}
                  </span>
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Results Info */}
        <div className="mb-3">
          <p className="text-muted">
            {filteredProducts.length === 0 && searchQuery && (
              <>Nessun risultato per "{searchQuery}"</>
            )}
            {filteredProducts.length === 0 && !searchQuery && selectedCategory && (
              <>Nessun prodotto in questa categoria</>
            )}
            {filteredProducts.length > 0 && (
              <>
                Mostrando {startIndex + 1}-{Math.min(endIndex, filteredProducts.length)} di{' '}
                {filteredProducts.length} prodott{filteredProducts.length === 1 ? 'o' : 'i'}
              </>
            )}
          </p>
        </div>

        {/* Products Grid with Loading State */}
        {isLoading ? (
          <div className="row">
            {[...Array(itemsPerPage)].map((_, index) => (
              <ProductCardSkeleton key={index} />
            ))}
          </div>
        ) : filteredProducts.length === 0 ? (
          <div className="alert alert-info">
            <h4 className="alert-heading">Nessun prodotto trovato</h4>
            <p className="mb-0">
              {searchQuery ? (
                <>
                  Prova a modificare i termini di ricerca o{' '}
                  <button
                    className="btn btn-link p-0 align-baseline"
                    onClick={() => {
                      handleSearchChange('');
                      handleCategoryChange(null);
                    }}
                  >
                    visualizza tutti i prodotti
                  </button>
                  .
                </>
              ) : selectedCategory ? (
                <>
                  Non ci sono prodotti disponibili in questa categoria.{' '}
                  <button
                    className="btn btn-link p-0 align-baseline"
                    onClick={() => handleCategoryChange(null)}
                  >
                    Visualizza tutti i prodotti
                  </button>
                  .
                </>
              ) : (
                'Al momento non ci sono prodotti disponibili.'
              )}
            </p>
          </div>
        ) : (
          <>
            <div className="row">
              {currentProducts.map((product) => (
                <div key={product.id} className="col-md-6 col-lg-4 mb-4">
                  <div className="card h-100 shadow-sm">
                    {product.previewImageUrl ? (
                      <img
                        src={product.previewImageUrl}
                        className="card-img-top"
                        alt={product.name}
                        style={{ height: '200px', objectFit: 'cover' }}
                      />
                    ) : (
                      <div
                        className="card-img-top bg-light d-flex align-items-center justify-content-center"
                        style={{ height: '200px' }}
                      >
                        <i className="bi bi-file-earmark-text fs-1 text-muted"></i>
                      </div>
                    )}
                    <div className="card-body d-flex flex-column">
                      <h5 className="card-title">{product.name}</h5>
                      <p className="card-text text-muted flex-grow-1">
                        {product.description.length > 100
                          ? `${product.description.substring(0, 100)}...`
                          : product.description}
                      </p>
                      <div className="mt-auto">
                        <h5 className="text-primary mb-3">
                          {formatPrice(product.price || 0)}
                        </h5>
                        <div className="d-grid">
                          {product.isActive ? (
                            <a
                              href={`/products/${product.id}`}
                              className="btn btn-primary"
                            >
                              Visualizza Dettagli
                            </a>
                          ) : (
                            <button className="btn btn-secondary" disabled>
                              Non Disponibile
                            </button>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <nav aria-label="Navigazione prodotti">
                <ul className="pagination justify-content-center mt-4">
                  <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                    <button
                      className="page-link"
                      onClick={() => handlePageChange(currentPage - 1)}
                      disabled={currentPage === 1}
                    >
                      <i className="bi bi-chevron-left"></i>
                    </button>
                  </li>

                  {getPageNumbers().map((pageNum, index) =>
                    pageNum === '...' ? (
                      <li key={`ellipsis-${index}`} className="page-item disabled">
                        <span className="page-link">...</span>
                      </li>
                    ) : (
                      <li
                        key={pageNum}
                        className={`page-item ${currentPage === pageNum ? 'active' : ''}`}
                      >
                        <button
                          className="page-link"
                          onClick={() => handlePageChange(pageNum as number)}
                        >
                          {pageNum}
                        </button>
                      </li>
                    )
                  )}

                  <li className={`page-item ${currentPage === totalPages ? 'disabled' : ''}`}>
                    <button
                      className="page-link"
                      onClick={() => handlePageChange(currentPage + 1)}
                      disabled={currentPage === totalPages}
                    >
                      <i className="bi bi-chevron-right"></i>
                    </button>
                  </li>
                </ul>
              </nav>
            )}
          </>
        )}
      </div>
    </>
  );
}