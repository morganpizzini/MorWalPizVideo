export enum QuestionType {
  TextArea = 0,
  Open = 0,
  MultipleChoice = 1,
  SingleChoice = 2,
  Boolean = 3,
}

export enum AnswerType {
  TextArea = 0,
  Open = 0,
  MultipleChoice = 1,
  SingleChoice = 2,
  Boolean = 3,
}

export interface QuestionOption {
  optionId: string;
  optionText: string;
  order: number;
}

// Base question interface
export interface CustomFormQuestion {
  _t?:
    | "OpenQuestion"
    | "MultipleChoiceQuestion"
    | "SingleChoiceQuestion"
    | "BooleanQuestion";
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

export interface BooleanQuestion extends CustomFormQuestion {
  _t?: "BooleanQuestion";
  questionType: QuestionType.Boolean;
}

// Union type for all question types
export type AnyQuestion =
  | OpenQuestion
  | MultipleChoiceQuestion
  | SingleChoiceQuestion
  | BooleanQuestion;

// Base answer interface
export interface CustomFormAnswer {
  _t?:
    | "OpenAnswer"
    | "MultipleChoiceAnswer"
    | "SingleChoiceAnswer"
    | "BooleanAnswer";
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

export interface BooleanAnswer extends CustomFormAnswer {
  _t?: "BooleanAnswer";
  answerType: AnswerType.Boolean;
  value: boolean;
}

// Union type for all answer types
export type AnyAnswer =
  | OpenAnswer
  | MultipleChoiceAnswer
  | SingleChoiceAnswer
  | BooleanAnswer;

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
