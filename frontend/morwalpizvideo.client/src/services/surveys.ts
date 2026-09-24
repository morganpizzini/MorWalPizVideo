import { getEligibleSurveys, getSurveyByUrl } from '@morwalpizvideo/services';
import type { Survey } from '@morwalpizvideo/models';

export const getPublicSurveys = (): Promise<Survey[]> => getEligibleSurveys();
export const getPublicSurveyByUrl = (url: string): Promise<Survey> => getSurveyByUrl(url);
