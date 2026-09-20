import { Link, useLoaderData } from 'react-router';
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
      <h1 className="accessories-page__title">ACCESSORI</h1>

      {Object.keys(columnGroups).length > 0 ? (
        <div className="accessories-grid">
          {Object.entries(columnGroups).map(([categoryId, categoryData]) => (
            <section
              key={categoryId}
              className="accessories-category"
              aria-labelledby={`category-${categoryId}`}
            >
              <h2 id={`category-${categoryId}`} className="-category__taccessoriesitle">
                {categoryData.title}
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
                    <Accordion className="accessories-products">
                      <ul className="accessories-products__list">
                        {subData.products.map((product: AccProduct, i: number) => (
                          <li key={product.id} className="accessories-product">
                            {product.description && product.description.length > 0 ? (
                              <Accordion.Item eventKey={`${categoryId}-${subId}-${i}`}>
                                <Accordion.Header>{product.title}</Accordion.Header>
                                <Accordion.Body>
                                  <p className="accessories-product__description">
                                    {product.description}
                                  </p>
                                  <Link
                                    to={product.url}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="accessories-product__link"
                                  >
                                    Vai al prodotto
                                  </Link>
                                </Accordion.Body>
                              </Accordion.Item>
                            ) : (
                              <Link
                                to={product.url}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="accessories-product__link"
                              >
                                {product.title}
                              </Link>
                            )}
                          </li>
                        ))}
                      </ul>
                    </Accordion>
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

import { useContext } from 'react';
import AccordionContext from 'react-bootstrap/AccordionContext';
import Accordion from 'react-bootstrap/Accordion';
import { useAccordionButton } from 'react-bootstrap/AccordionButton';

interface CustomToggleProps {
  children: React.ReactNode;
  eventKey: string;
  callback?: (eventKey: string) => void;
}

export function CustomToggle({ children, eventKey, callback }: CustomToggleProps) {
  const { activeEventKey } = useContext(AccordionContext);

  const decoratedOnClick = useAccordionButton(eventKey, () => callback && callback(eventKey));

  const isCurrentEventKey = activeEventKey === eventKey;

  return (
    <button
      type="button"
      className="w-100 border-0"
      style={{ backgroundColor: 'transparent' }}
      onClick={decoratedOnClick}
    >
      <i className={`fa ${isCurrentEventKey ? 'fa-chevron-down' : 'fa-chevron-right'} me-1`}></i>
      {children}
    </button>
  );
}
