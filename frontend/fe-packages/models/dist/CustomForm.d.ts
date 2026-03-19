export declare enum QuestionType {
    Open = 0,
    MultipleChoice = 1,
    SingleChoice = 2
}
export declare enum AnswerType {
    Open = 0,
    MultipleChoice = 1,
    SingleChoice = 2
}
export interface QuestionOption {
    optionId: string;
    optionText: string;
    order: number;
}
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
export type AnyQuestion = OpenQuestion | MultipleChoiceQuestion | SingleChoiceQuestion;
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
    questions: AnyQuestion[];
    responses: CustomFormResponse[];
    responseCount: number;
    creationDateTime: string;
}
export interface CreateCustomFormRequest {
    title: string;
    description: string;
    questions: AnyQuestion[];
}
export interface UpdateCustomFormRequest {
    id: string;
    title: string;
    description: string;
    questions: AnyQuestion[];
}
export interface SubmitFormResponseRequest {
    answers: AnyAnswer[];
}
//# sourceMappingURL=CustomForm.d.ts.map