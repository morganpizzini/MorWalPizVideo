import { Link, useLoaderData } from "react-router-dom";
import { useContext } from 'react';
import AccordionContext from 'react-bootstrap/AccordionContext';
import { getProducts } from "@services/products";
import Accordion from 'react-bootstrap/Accordion';
import { useAccordionButton } from 'react-bootstrap/AccordionButton';
export async function loader() {
    const products = await getProducts();
    return { products };
}
export default function Accessories() {
    const { products } = useLoaderData();

    const equipment = products.filter(product => product.category === 'attrezzatura')
        .reduce((acc, product) => {
            if (!acc[product.category2]) {
                acc[product.category2] = [];
            }
            acc[product.category2].push(product);
            return acc;
        }, {});
    const video = products.filter(product => product.category === 'registrazione')
        .reduce((acc, product) => {
            if (!acc[product.category2]) {
                acc[product.category2] = [];
            }
            acc[product.category2].push(product);
            return acc;
        }, {});
    return (
        <>
            <h1 className="text-center mb-3">ACCESSORI</h1>



            <div className="row text-center">
                <div className="col-md-6 border-end border-black">
                    <ul className="list-group">
                        <li className="list-group-item"><h2>⚙️&nbsp;Attrezzatura&nbsp;⚙️</h2></li>
                        {Object.keys(equipment).map((group) => (
                            <li key={group} className="list-group-item">
                                <span className="fw-bold pt-2 pb-1">{group}</span>
                                <Accordion>
                                    <ul className="ps-0">
                                        {equipment[group].map((product, i) => (
                                            <li key={product.title} className="list-group-item">
                                                {product.description.length > 0 &&
                                                    <>
                                                        <Accordion.Item eventKey={i}>
                                                            <Accordion.Header>{product.title}</Accordion.Header>
                                                            <Accordion.Body>
                                                                {product.description}
                                                                <p className="text-end">
                                                                    <Link to={product.url} target="_blank">Vai al prodotto</Link>
                                                                </p>
                                                            </Accordion.Body>
                                                        </Accordion.Item>
                                                    </>
                                                }
                                                {product.description.length == 0 &&
                                                    <>
                                                        {product.title}<Link className="stretched-link" to={product.url} target="_blank"></Link>
                                                    </>
                                                }
                                            </li>
                                        ))}
                                    </ul>
                                </Accordion>
                            </li>
                        ))}
                    </ul>
                </div>
                <div className="col-md-6 border-start border-black">
                    <ul className="list-group">
                        <li className="list-group-item"><h2>📹&nbsp;Video&nbsp;📹</h2></li>
                        {Object.keys(video).map((group) => (
                            <li key={group} className="list-group-item">
                                <span className="fw-bold pt-2 pb-1">{group}</span>
                                <Accordion>
                                    <ul className="ps-0">
                                        {video[group].map((product, i) => (
                                            <li key={product.title} className="list-group-item">
                                                {product.description.length > 0 &&
                                                    <>
                                                    <CustomToggle eventKey={i}>{product.title}</CustomToggle>
                                                        <Accordion.Collapse eventKey={i}>
                                                        <>
                                                            <small>{product.description}</small>
                                                                <p className="text-end">
                                                                    <Link to={product.url} target="_blank">Vai al prodotto</Link>
                                                                </p>
                                                            </>
                                                        </Accordion.Collapse>
                                                    </>
                                                }
                                                {product.description.length == 0 &&
                                                    <>
                                                        {product.title}<Link className="stretched-link" to={product.url} target="_blank"></Link>
                                                    </>
                                                }
                                            </li>
                                        ))}
                                    </ul>
                                </Accordion>
                            </li>
                        ))}
                    </ul>
                </div>
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