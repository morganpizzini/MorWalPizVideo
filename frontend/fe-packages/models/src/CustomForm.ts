export enum QuestionType {
  Open = 0,
  MultipleChoice = 1,
  SingleChoice = 2
}

export enum AnswerType {
  Open = 0,
  MultipleChoice = 1,
  SingleChoice = 2
}

export interface QuestionOption {
  optionId: string;
  optionText: string;
  order: number;
}

// Base question interface
export interface CustomFormQuestion {
  questionId: string;
  questionText: string;
  questionType: QuestionType;
  isRequired: boolean;
  order: number;
}

export interface OpenQuestion extends CustomFormQuestion {
  questionType: QuestionType.Open;
}

export interface MultipleChoiceQuestion extends CustomFormQuestion {
  questionType: QuestionType.MultipleChoice;
  options: QuestionOption[];
}

export interface SingleChoiceQuestion extends CustomFormQuestion {
  questionType: QuestionType.SingleChoice;
  options: QuestionOption[];
}

// Union type for all question types
export type AnyQuestion = OpenQuestion | MultipleChoiceQuestion | SingleChoiceQuestion;

// Base answer interface
export interface CustomFormAnswer {
  questionId: string;
  answerType: AnswerType;
}

export interface OpenAnswer extends CustomFormAnswer {
  answerType: AnswerType.Open;
  textResponse: string;
}

export interface MultipleChoiceAnswer extends CustomFormAnswer {
  answerType: AnswerType.MultipleChoice;
  selectedOptionIds: string[];
}

export interface SingleChoiceAnswer extends CustomFormAnswer {
  answerType: AnswerType.SingleChoice;
  selectedOptionId: string;
}

// Union type for all answer types
export type AnyAnswer = OpenAnswer | MultipleChoiceAnswer | SingleChoiceAnswer;

export interface CustomFormResponse {
  responseId: string;
  submittedAt: string;
  answers: AnyAnswer[];
}

export interface CustomForm {
  id: string;
  title: string;
  description: string;
  url: string;
  active: boolean;
  questions: AnyQuestion[];
  responses: CustomFormResponse[];
  responseCount: number;
  creationDateTime: string;
}

// Request DTOs
export interface CreateCustomFormRequest {
  title: string;
  description: string;
  url: string;
  questions: AnyQuestion[];
  active: boolean;
}

export interface UpdateCustomFormRequest {
  id: string;
  title: string;
  description: string;
  url: string;
  questions: AnyQuestion[];
  active: boolean;
}

export interface SubmitFormResponseRequest {
  answers: AnyAnswer[];
}
