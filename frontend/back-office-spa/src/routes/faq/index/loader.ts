import { fetchFaqCategories, fetchFaqs } from '@morwalpizvideo/services';
import type { FaqAdmin, FaqCategoryAdmin } from '@morwalpizvideo/models';

export interface FaqIndexData {
  faqs: FaqAdmin[];
  categories: FaqCategoryAdmin[];
}

export default async function loader(): Promise<FaqIndexData> {
  const [faqs, categories] = await Promise.all([fetchFaqs(), fetchFaqCategories()]);
  if (!Array.isArray(faqs) || !Array.isArray(categories)) throw new Response('Unable to load FAQs', { status: 502 });
  return { faqs, categories };
}
