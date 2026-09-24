import { get, post, frontendEndpoints, requireSuccessfulResponse } from '@morwalpizvideo/services';
import type { AnyAnswer, CustomForm, OpenAnswer } from '@morwalpizvideo/models';

export function getSponsors() {
  return get(frontendEndpoints.SPONSORS);
}

export function askForSponsor(name: string, email: string, description: string, token: string) {
  return post(frontendEndpoints.SPONSORS, {
    name,
    email,
    description,
    token,
  })
    .then(requireSuccessfulResponse)
    .then((response) => {
      // Handle 204 No Content response
      if (!response || Object.keys(response).length === 0) {
        return true;
      }
      return response;
    });
}

const sponsorQuestionLabels = {
  name: new Set(['name', 'nome']),
  email: new Set(['email']),
  description: new Set(['description', 'descrizione']),
} as const;

function normalizeQuestionText(value: string): string {
  return value.trim().toLocaleLowerCase().replace(/\s+/g, ' ');
}

export function getSponsorRequestFromForm(
  form: CustomForm,
  answers: AnyAnswer[],
  recaptchaToken: string | null
): { name: string; email: string; description: string; token: string } {
  if (!recaptchaToken) {
    throw new Error('Unable to submit sponsor request because reCAPTCHA is unavailable.');
  }

  const fields = Object.entries(sponsorQuestionLabels).map(([field, labels]) => {
    const matches = form.questions.filter(
      (question) =>
        question.questionType === 0 && labels.has(normalizeQuestionText(question.questionText))
    );
    if (matches.length !== 1) {
      throw new Error(`Sponsor form must contain exactly one ${field} question.`);
    }

    const answer = answers.find((item) => item.questionId === matches[0].questionId);
    if (!answer || answer.answerType !== 0) {
      throw new Error(`Sponsor ${field} answer is missing.`);
    }

    return [field, (answer as OpenAnswer).textResponse.trim()] as const;
  });

  return {
    name: fields.find(([field]) => field === 'name')![1],
    email: fields.find(([field]) => field === 'email')![1],
    description: fields.find(([field]) => field === 'description')![1],
    token: recaptchaToken,
  };
}
