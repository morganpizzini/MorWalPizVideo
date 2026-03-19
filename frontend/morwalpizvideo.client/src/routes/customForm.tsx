import { useState } from 'react';
import { useLoaderData } from 'react-router';
import { submitFormResponse } from '../services/customForms';
import ReactGA from 'react-ga4';

const QuestionType = {
  Open: 0,
  MultipleChoice: 1,
  SingleChoice: 2
};

const AnswerType = {
  Open: 0,
  MultipleChoice: 1,
  SingleChoice: 2
};

export default function CustomForm() {
  const { form } = useLoaderData();
  const [answers, setAnswers] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [error, setError] = useState(null);

  // Track page view
  useState(() => {
    ReactGA.send({ hitType: 'pageview', page: `/forms/${form.url}`, title: form.title });
  }, []);

  const handleAnswerChange = (questionId, value, questionType) => {
    setAnswers(prev => ({
      ...prev,
      [questionId]: {
        questionId,
        answerType: questionType,
        ...value
      }
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);

    // Validate all required questions are answered
    const unansweredRequired = form.questions
      .filter(q => q.isRequired)
      .find(q => {
        const answer = answers[q.questionId];
        if (!answer) return true;
        
        if (q.questionType === QuestionType.Open) {
          return !answer.textResponse || answer.textResponse.trim() === '';
        } else if (q.questionType === QuestionType.MultipleChoice) {
          return !answer.selectedOptionIds || answer.selectedOptionIds.length === 0;
        } else if (q.questionType === QuestionType.SingleChoice) {
          return !answer.selectedOptionId;
        }
        return false;
      });

    if (unansweredRequired) {
      setError(`Please answer the required question: "${unansweredRequired.questionText}"`);
      return;
    }

    // Convert answers object to array
    const answersArray = form.questions.map(q => {
      const answer = answers[q.questionId];
      if (!answer) {
        // Provide empty answer for non-required questions
        if (q.questionType === QuestionType.Open) {
          return {
            questionId: q.questionId,
            answerType: AnswerType.Open,
            textResponse: ''
          };
        } else if (q.questionType === QuestionType.MultipleChoice) {
          return {
            questionId: q.questionId,
            answerType: AnswerType.MultipleChoice,
            selectedOptionIds: []
          };
        } else {
          return {
            questionId: q.questionId,
            answerType: AnswerType.SingleChoice,
            selectedOptionId: ''
          };
        }
      }
      return answer;
    });

    setIsSubmitting(true);

    try {
      await submitFormResponse(form.id, answersArray);
      setIsSubmitted(true);
      
      // Track form submission
      ReactGA.event({
        category: 'Form',
        action: 'Submit',
        label: form.title
      });
    } catch (err) {
      setError(err.message || 'Failed to submit form. Please try again.');
      console.error('Error submitting form:', err);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isSubmitted) {
    return (
      <div className="container my-5">
        <div className="row justify-content-center">
          <div className="col-md-8">
            <div className="card shadow">
              <div className="card-body text-center py-5">
                <div className="mb-4">
                  <svg width="64" height="64" fill="currentColor" className="text-success" viewBox="0 0 16 16">
                    <path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/>
                  </svg>
                </div>
                <h2 className="mb-3">Thank You!</h2>
                <p className="text-muted">Your response has been submitted successfully.</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container my-5">
      <div className="row justify-content-center">
        <div className="col-md-8">
          <div className="card shadow">
            <div className="card-header bg-primary text-white">
              <h1 className="h3 mb-0">{form.title}</h1>
            </div>
            <div className="card-body">
              {form.description && (
                <p className="text-muted mb-4">{form.description}</p>
              )}

              {error && (
                <div className="alert alert-danger" role="alert">
                  {error}
                </div>
              )}

              <form onSubmit={handleSubmit}>
                {form.questions.map((question, index) => (
                  <div key={question.questionId} className="mb-4 pb-4 border-bottom">
                    <label className="form-label fw-bold">
                      {index + 1}. {question.questionText}
                      {question.isRequired && <span className="text-danger ms-1">*</span>}
                    </label>

                    {question.questionType === QuestionType.Open && (
                      <textarea
                        className="form-control"
                        rows="4"
                        placeholder="Enter your answer..."
                        required={question.isRequired}
                        onChange={(e) => handleAnswerChange(
                          question.questionId,
                          { textResponse: e.target.value },
                          AnswerType.Open
                        )}
                      />
                    )}

                    {question.questionType === QuestionType.SingleChoice && (
                      <div>
                        {question.options.map(option => (
                          <div key={option.optionId} className="form-check">
                            <input
                              className="form-check-input"
                              type="radio"
                              name={question.questionId}
                              id={option.optionId}
                              required={question.isRequired}
                              onChange={(e) => {
                                if (e.target.checked) {
                                  handleAnswerChange(
                                    question.questionId,
                                    { selectedOptionId: option.optionId },
                                    AnswerType.SingleChoice
                                  );
                                }
                              }}
                            />
                            <label className="form-check-label" htmlFor={option.optionId}>
                              {option.optionText}
                            </label>
                          </div>
                        ))}
                      </div>
                    )}

                    {question.questionType === QuestionType.MultipleChoice && (
                      <div>
                        {question.options.map(option => (
                          <div key={option.optionId} className="form-check">
                            <input
                              className="form-check-input"
                              type="checkbox"
                              id={option.optionId}
                              onChange={(e) => {
                                const currentAnswer = answers[question.questionId];
                                const currentSelected = currentAnswer?.selectedOptionIds || [];
                                const newSelected = e.target.checked
                                  ? [...currentSelected, option.optionId]
                                  : currentSelected.filter(id => id !== option.optionId);
                                
                                handleAnswerChange(
                                  question.questionId,
                                  { selectedOptionIds: newSelected },
                                  AnswerType.MultipleChoice
                                );
                              }}
                            />
                            <label className="form-check-label" htmlFor={option.optionId}>
                              {option.optionText}
                            </label>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                ))}

                <div className="d-grid">
                  <button
                    type="submit"
                    className="btn btn-primary btn-lg"
                    disabled={isSubmitting}
                  >
                    {isSubmitting ? 'Submitting...' : 'Submit'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
