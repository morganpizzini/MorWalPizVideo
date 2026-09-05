import { fetchFaqCategories, getFaq } from '@morwalpizvideo/services';
import type { FaqAdmin, FaqCategoryAdmin } from '@morwalpizvideo/models';

export interface FaqFormData { faq: FaqAdmin | null; categories: FaqCategoryAdmin[]; }

export default async function loader({ params }: { params: { id?: string } }): Promise<FaqFormData> {
  const [faq, categories] = await Promise.all([
    params.id ? getFaq(params.id) : Promise.resolve(null),
    fetchFaqCategories(),
  ]);
  return { faq, categories };
}
