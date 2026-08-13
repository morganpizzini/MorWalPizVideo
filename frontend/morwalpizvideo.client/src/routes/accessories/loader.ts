import { getProducts } from "@services/products";

export default async function loader() {
    const products = await getProducts();
    return { products };
}