﻿import { Link, useLoaderData } from "react-router-dom";
import { getProducts } from "@services/products";

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
        }, {})        ;
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
                                <ul className="ps-0">
                                    {equipment[group].map(product => (
                                        <li key={product.title} className="list-group-item">{product.title}<Link className="stretched-link" to={product.url} target="_blank"></Link></li>
                                    ))}
                                </ul>
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
                                <ul className="ps-0">
                                    {video[group].map(product => (
                                        <li key={product.title} className="list-group-item">{product.title}<Link className="stretched-link" to={product.url} target="_blank"></Link></li>
                                    ))}
                                </ul>
                            </li>
                        ))}
                    </ul>
                </div>
            </div>
        </>
    );
}