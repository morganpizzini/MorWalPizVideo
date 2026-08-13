import { useState } from 'react';
import type { AnyAnswer, AnyQuestion, CustomForm } from '@morwalpizvideo/models';
import { AnswerType, QuestionType } from '@morwalpizvideo/models';

export interface CustomFormRendererProps {
  form: CustomForm;
  onSubmit: (answers: AnyAnswer[], recaptchaToken: string | null) => Promise<void>;
  getRecaptchaToken: () => Promise<string | null>;
}

function emptyAnswer(question: AnyQuestion): AnyAnswer {
  switch (question.questionType) {
    case QuestionType.Open:
      return { questionId: question.questionId, answerType: AnswerType.Open, textResponse: '' };
    case QuestionType.MultipleChoice:
      return { questionId: question.questionId, answerType: AnswerType.MultipleChoice, selectedOptionIds: [] };
    case QuestionType.SingleChoice:
      return { questionId: question.questionId, answerType: AnswerType.SingleChoice, selectedOptionId: '' };
  }
}

export function CustomFormRenderer({ form, onSubmit, getRecaptchaToken }: CustomFormRendererProps) {
  const [answers, setAnswers] = useState<Record<string, AnyAnswer>>({});
  const [error, setError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const updateAnswer = (question: AnyQuestion, answer: AnyAnswer) => setAnswers(previous => ({ ...previous, [question.questionId]: answer }));

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const orderedAnswers = form.questions.slice().sort((left: AnyQuestion, right: AnyQuestion) => left.order - right.order).map((question: AnyQuestion) => answers[question.questionId] ?? emptyAnswer(question));
    const unanswered = form.questions.find((question: AnyQuestion) => {
      const answer = orderedAnswers.find((item: AnyAnswer) => item.questionId === question.questionId)!;
      if (!question.isRequired) return false;
      if (answer.answerType === AnswerType.Open) return !answer.textResponse.trim();
      if (answer.answerType === AnswerType.MultipleChoice) return answer.selectedOptionIds.length === 0;
      return !answer.selectedOptionId;
    });
    if (unanswered) {
      setError(`Please answer the required question: "${unanswered.questionText}"`);
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      const recaptchaToken = await getRecaptchaToken();
      await onSubmit(orderedAnswers, recaptchaToken);
      setSubmitted(true);
    } catch (submissionError) {
      setError(submissionError instanceof Error ? submissionError.message : 'Failed to submit form.');
    } finally {
      setSubmitting(false);
    }
  };

  if (submitted) return <div className="alert alert-success" role="status">Your response has been submitted successfully.</div>;

  return (
    <form onSubmit={submit}>
      <h1>{form.title}</h1>
      {form.description && <p className="text-muted">{form.description}</p>}
      {error && <div className="alert alert-danger" role="alert">{error}</div>}
      {form.questions.slice().sort((left: AnyQuestion, right: AnyQuestion) => left.order - right.order).map((question: AnyQuestion, index: number) => {
        const answer = answers[question.questionId] ?? emptyAnswer(question);
        return (
          <fieldset key={question.questionId} className="mb-4">
            <legend className="h5">{index + 1}. {question.questionText}{question.isRequired && <span className="text-danger"> *</span>}</legend>
            {question.questionType === QuestionType.Open && <textarea className="form-control" rows={4} value={answer.answerType === AnswerType.Open ? answer.textResponse : ''} onChange={event => updateAnswer(question, { questionId: question.questionId, answerType: AnswerType.Open, textResponse: event.target.value })} />}
            {(question.questionType === QuestionType.SingleChoice || question.questionType === QuestionType.MultipleChoice) && question.options.map((option: { optionId: string; optionText: string }) => {
              const checked = question.questionType === QuestionType.SingleChoice ? answer.answerType === AnswerType.SingleChoice && answer.selectedOptionId === option.optionId : answer.answerType === AnswerType.MultipleChoice && answer.selectedOptionIds.includes(option.optionId);
              return <label key={option.optionId} className="d-block form-check mb-2"><input className="form-check-input" type={question.questionType === QuestionType.SingleChoice ? 'radio' : 'checkbox'} name={question.questionId} checked={checked} onChange={event => {
                if (question.questionType === QuestionType.SingleChoice) updateAnswer(question, { questionId: question.questionId, answerType: AnswerType.SingleChoice, selectedOptionId: option.optionId });
                else {
                  const selected = answer.answerType === AnswerType.MultipleChoice ? answer.selectedOptionIds : [];
                  updateAnswer(question, { questionId: question.questionId, answerType: AnswerType.MultipleChoice, selectedOptionIds: event.target.checked ? [...selected, option.optionId] : selected.filter((id: string) => id !== option.optionId) });
                }
              }} /> {option.optionText}</label>;
            })}
          </fieldset>
        );
      })}
      <button type="submit" className="btn btn-primary" disabled={submitting}>{submitting ? 'Submitting...' : 'Submit'}</button>
      <p className="small text-muted mt-3">This site is protected by reCAPTCHA and the Google Privacy Policy and Terms of Service.</p>
    </form>
  );
}