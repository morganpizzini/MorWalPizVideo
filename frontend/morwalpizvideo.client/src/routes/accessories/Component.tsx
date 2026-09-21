import { Link, useLoaderData } from 'react-router';
import { PageTitle } from '@morwalpiz/layout';
import './style.scss';

interface AccCategory {
  id: string;
  title: string;
}
interface AccProduct {
  id: string;
  title: string;
  description?: string;
  url: string;
  categories?: AccCategory[];
}
interface AccSubGroup {
  title: string | null;
  products: AccProduct[];
}
interface AccColumnGroup {
  title: string;
  id: string;
  subcategories: Record<string, AccSubGroup>;
}

export default function Accessories() {
  const { products } = useLoaderData() as { products: AccProduct[] };

  // Group products by main category (first category) and subcategory (second category)
  const columnGroups = products.reduce<Record<string, AccColumnGroup>>((acc, product) => {
    if (!product.categories || product.categories.length === 0) {
      return acc; // Skip products without categories
    }

    const mainCategory = product.categories[0];
    const subCategory = product.categories[1] || null;

    if (!acc[mainCategory.id]) {
      acc[mainCategory.id] = {
        title: mainCategory.title,
        id: mainCategory.id,
        subcategories: {},
      };
    }

    const subCategoryKey = subCategory ? subCategory.id : 'default';
    if (!acc[mainCategory.id].subcategories[subCategoryKey]) {
      acc[mainCategory.id].subcategories[subCategoryKey] = {
        title: subCategory ? subCategory.title : null,
        products: [],
      };
    }

    acc[mainCategory.id].subcategories[subCategoryKey].products.push(product);
    return acc;
  }, {});

  return (
    <main className="accessories-page">
      <PageTitle
        title="ACCESSORI"
        subtitle="Accessori e strumenti selezionati per migliorare ogni sessione."
      />

      {Object.keys(columnGroups).length > 0 ? (
        <div className="accessories-grid">
          {Object.entries(columnGroups).map(([categoryId, categoryData]) => (
            <section
              key={categoryId}
              className="accessories-category"
              aria-labelledby={`category-${categoryId}`}
            >
              <h2 id={`category-${categoryId}`} className="accessories-category__title">
                {categoryData.title}
                <span className="accessories-category__count">
                  {Object.values(categoryData.subcategories).reduce(
                    (count, subcategory) => count + subcategory.products.length,
                    0
                  )}{' '}
                  prodotti
                </span>
              </h2>
              <div className="accessories-subcategories">
                {Object.entries(categoryData.subcategories).map(([subId, subData]) => (
                  <section
                    key={subId}
                    className="accessories-subcategory"
                    aria-labelledby={`subcategory-${categoryId}-${subId}`}
                  >
                    {subData.title && (
                      <h3
                        id={`subcategory-${categoryId}-${subId}`}
                        className="accessories-subcategory__title"
                      >
                        {subData.title}
                      </h3>
                    )}
                    <ul className="accessories-products__list">
                      {subData.products.map((product: AccProduct) => (
                        <li key={product.id} className="accessories-product">
                          <div className="accessories-product__details">
                            <h4 className="accessories-product__title">{product.title}</h4>
                            {product.description && (
                              <p className="accessories-product__description">
                                {product.description}
                              </p>
                            )}
                          </div>
                          <Link
                            to={product.url}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="accessories-product__link"
                          >
                            Vai al prodotto <span aria-hidden="true">&#8594;</span>
                          </Link>
                        </li>
                      ))}
                    </ul>
                  </section>
                ))}
              </div>
            </section>
          ))}
        </div>
      ) : (
        <p className="accessories-page__empty" role="status">
          Nessun accessorio disponibile.
        </p>
      )}
    </main>
  );
}
