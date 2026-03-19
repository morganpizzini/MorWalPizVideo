import { Link, useLoaderData } from "react-router";
import { useContext } from 'react';
import AccordionContext from 'react-bootstrap/AccordionContext';
import Accordion from 'react-bootstrap/Accordion';
import { useAccordionButton } from 'react-bootstrap/AccordionButton';

export default function Accessories() {
    const { products } = useLoaderData();

    // Group products by main category (first category) and subcategory (second category)
    const columnGroups = products.reduce((acc, product) => {
        if (!product.categories || product.categories.length === 0) {
            return acc; // Skip products without categories
        }

        const mainCategory = product.categories[0];
        const subCategory = product.categories[1] || null;

        if (!acc[mainCategory.id]) {
            acc[mainCategory.id] = {
                title: mainCategory.title,
                id: mainCategory.id,
                subcategories: {}
            };
        }

        const subCategoryKey = subCategory ? subCategory.id : 'default';
        if (!acc[mainCategory.id].subcategories[subCategoryKey]) {
            acc[mainCategory.id].subcategories[subCategoryKey] = {
                title: subCategory ? subCategory.title : null,
                products: []
            };
        }

        acc[mainCategory.id].subcategories[subCategoryKey].products.push(product);
        return acc;
    }, {});

    // Calculate column width based on number of main categories
    const columnCount = Object.keys(columnGroups).length;
    const columnClass = columnCount > 0 ? `col-md-${Math.floor(12 / columnCount)}` : 'col-md-12';

    return (
        <>
            <h1 className="text-center mb-3">ACCESSORI</h1>

            <div className="row text-center">
                {Object.entries(columnGroups).map(([categoryId, categoryData], index, arr) => (
                    <div 
                        key={categoryId} 
                        className={`${columnClass} ${index < arr.length - 1 ? 'border-end border-black' : ''}`}
                    >
                        <ul className="list-group">
                            <li className="list-group-item">
                                <h2>{categoryData.title}</h2>
                            </li>
                            {Object.entries(categoryData.subcategories).map(([subId, subData]) => (
                                <li key={subId} className="list-group-item">
                                    {subData.title && (
                                        <span className="fw-bold pt-2 pb-1">{subData.title}</span>
                                    )}
                                    <Accordion>
                                        <ul className="ps-0">
                                            {subData.products.map((product, i) => (
                                                <li key={product.id} className="list-group-item">
                                                    {product.description && product.description.length > 0 ? (
                                                        <>
                                                            <Accordion.Item eventKey={`${categoryId}-${subId}-${i}`}>
                                                                <Accordion.Header>{product.title}</Accordion.Header>
                                                                <Accordion.Body>
                                                                    {product.description}
                                                                    <p className="text-end">
                                                                        <Link to={product.url} target="_blank">Vai al prodotto</Link>
                                                                    </p>
                                                                </Accordion.Body>
                                                            </Accordion.Item>
                                                        </>
                                                    ) : (
                                                        <>
                                                            {product.title}<Link className="stretched-link" to={product.url} target="_blank"></Link>
                                                        </>
                                                    )}
                                                </li>
                                            ))}
                                        </ul>
                                    </Accordion>
                                </li>
                            ))}
                        </ul>
                    </div>
                ))}
            </div>
        </>
    );
}

function CustomToggle({ children, eventKey, callback }) {
    const { activeEventKey } = useContext(AccordionContext);

    const decoratedOnClick = useAccordionButton(
        eventKey,
        () => callback && callback(eventKey),
    );

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
