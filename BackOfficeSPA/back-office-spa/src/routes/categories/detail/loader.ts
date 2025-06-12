import { Category } from '@models';

export default async function loader({ params }: { params: { id: string } }): Promise<Category> {
    return fetch(`/api/categories/${params.id}`).then(response => response.json());
}
