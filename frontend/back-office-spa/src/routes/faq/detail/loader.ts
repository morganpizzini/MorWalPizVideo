import { fetchFaqAnswers, fetchFaqCategories, getFaq } from '@morwalpizvideo/services';
import type { FaqAdmin, FaqAnswerAdmin, FaqCategoryAdmin } from '@morwalpizvideo/models';

export interface FaqDetailData { faq: FaqAdmin; answers: FaqAnswerAdmin[]; categories: FaqCategoryAdmin[]; }

export default async function loader({ params }: { params: { id?: string } }): Promise<FaqDetailData> {
  if (!params.id) throw new Response('FAQ not found', { status: 404 });
  const [faq, answers, categories] = await Promise.all([getFaq(params.id), fetchFaqAnswers(params.id), fetchFaqCategories()]);
  return { faq, answers, categories };
}
