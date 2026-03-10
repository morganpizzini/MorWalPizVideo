import React, { useState } from 'react';
import { Button, Card, Modal, Badge, Accordion, Table } from 'react-bootstrap';
import { Link, useLoaderData, useFetcher, useNavigate } from 'react-router';
import { 
  CustomForm, 
  QuestionType, 
  AnyQuestion, 
  MultipleChoiceQuestion, 
  SingleChoiceQuestion,
  CustomFormResponse,
  AnyAnswer,
  AnswerType,
  OpenAnswer,
  MultipleChoiceAnswer,
  SingleChoiceAnswer
} from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';

const CustomFormDetail: React.FC = () => {
  const form = useLoaderData() as CustomForm;
  const [showModal, setShowModal] = useState(false);
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;
  const result =
    fetcher.data != undefined &&
      (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
      ? fetcher.data
      : null;

  React.useEffect(() => {
    if (!result) return;
    setShowModal(false);

    if (result.success) {
      toast.show('Success', 'Custom form deleted successfully', { variant: 'success' });
      navigate('/customforms');
    }
  }, [result, navigate]);

  const handleDelete = () => {
    setShowModal(true);
  };

  const confirmDelete = () => {
    fetcher.submit(
      {
        id: form.id,
      },
      {
        method: 'post',
        action: `/customforms`
      }
    );
  };

  const getQuestionTypeBadge = (type: QuestionType) => {
    switch (type) {
      case QuestionType.Open:
        return <Badge bg="primary">Open Text</Badge>;
      case QuestionType.MultipleChoice:
        return <Badge bg="info">Multiple Choice</Badge>;
      case QuestionType.SingleChoice:
        return <Badge bg="success">Single Choice</Badge>;
      default:
        return <Badge bg="secondary">Unknown</Badge>;
    }
  };

  const renderQuestionDetails = (question: AnyQuestion) => {
    return (
      <Card className="mb-2">
        <Card.Body>
          <div className="d-flex justify-content-between align-items-start mb-2">
            <div>
              <strong>{question.questionText}</strong>
              {question.isRequired && <Badge bg="danger" className="ms-2">Required</Badge>}
            </div>
            {getQuestionTypeBadge(question.questionType)}
          </div>
          
          {(question.questionType === QuestionType.MultipleChoice || 
            question.questionType === QuestionType.SingleChoice) && (
            <div className="mt-2">
              <small className="text-muted">Options:</small>
              <ul className="mb-0 mt-1">
                {(question as MultipleChoiceQuestion | SingleChoiceQuestion).options
                  .sort((a, b) => a.order - b.order)
                  .map(opt => (
                    <li key={opt.optionId}>{opt.optionText}</li>
                  ))}
              </ul>
            </div>
          )}
        </Card.Body>
      </Card>
    );
  };

  const renderAnswerValue = (answer: AnyAnswer, question: AnyQuestion) => {
    switch (answer.answerType) {
      case AnswerType.Open:
        return <div>{(answer as OpenAnswer).textResponse || <em>No response</em>}</div>;
      
      case AnswerType.SingleChoice:
        const singleAnswer = answer as SingleChoiceAnswer;
        const singleQuestion = question as SingleChoiceQuestion;
        const selectedOption = singleQuestion.options.find(
          opt => opt.optionId === singleAnswer.selectedOptionId
        );
        return <div>{selectedOption?.optionText || <em>Unknown option</em>}</div>;
      
      case AnswerType.MultipleChoice:
        const multiAnswer = answer as MultipleChoiceAnswer;
        const multiQuestion = question as MultipleChoiceQuestion;
        const selectedOptions = multiQuestion.options.filter(
          opt => multiAnswer.selectedOptionIds.includes(opt.optionId)
        );
        return (
          <div>
            {selectedOptions.length > 0 ? (
              selectedOptions.map(opt => (
                <Badge key={opt.optionId} bg="secondary" className="me-1">
                  {opt.optionText}
                </Badge>
              ))
            ) : (
              <em>No options selected</em>
            )}
          </div>
        );
      
      default:
        return <em>Unknown answer type</em>;
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  return (
    <>
      <PageHeader title="Custom Form Details" />
      <GenericErrorList errors={errors?.generics} />

      <Card className="mb-4">
        <Card.Body>
          <div className="d-flex justify-content-between align-items-center mb-3">
            <h2>{form.title}</h2>
            <div>
              <Link
                to={`/customforms/${form.id}/edit`}
                className="btn btn-primary me-2"
              >
                Edit
              </Link>
              <Button variant="danger" onClick={handleDelete}>
                Delete
              </Button>
            </div>
          </div>

          <dl className="row">
            <dt className="col-sm-3">Description</dt>
            <dd className="col-sm-9">
              {form.description || <em>No description provided</em>}
            </dd>

            <dt className="col-sm-3">Created</dt>
            <dd className="col-sm-9">{formatDate(form.creationDateTime)}</dd>

            <dt className="col-sm-3">Questions</dt>
            <dd className="col-sm-9">
              <Badge bg="info">{form.questions.length}</Badge>
            </dd>

            <dt className="col-sm-3">Responses</dt>
            <dd className="col-sm-9">
              <Badge bg={form.responseCount > 0 ? 'success' : 'secondary'}>
                {form.responseCount}
              </Badge>
            </dd>
          </dl>
        </Card.Body>
      </Card>

      <Card className="mb-4">
        <Card.Header>
          <h5 className="mb-0">Questions</h5>
        </Card.Header>
        <Card.Body>
          {form.questions.length > 0 ? (
            form.questions
              .sort((a, b) => a.order - b.order)
              .map((question, index) => (
                <div key={question.questionId}>
                  <div className="mb-2">
                    <Badge bg="secondary">Question {index + 1}</Badge>
                  </div>
                  {renderQuestionDetails(question)}
                </div>
              ))
          ) : (
            <em>No questions defined</em>
          )}
        </Card.Body>
      </Card>

      {form.responses.length > 0 && (
        <Card className="mb-4">
          <Card.Header>
            <h5 className="mb-0">Responses ({form.responseCount})</h5>
          </Card.Header>
          <Card.Body>
            <Accordion>
              {form.responses.map((response, index) => (
                <Accordion.Item key={response.responseId} eventKey={index.toString()}>
                  <Accordion.Header>
                    Response #{index + 1} - {formatDate(response.submittedAt)}
                  </Accordion.Header>
                  <Accordion.Body>
                    <Table bordered hover size="sm">
                      <thead>
                        <tr>
                          <th style={{ width: '40%' }}>Question</th>
                          <th>Answer</th>
                        </tr>
                      </thead>
                      <tbody>
                        {form.questions
                          .sort((a, b) => a.order - b.order)
                          .map(question => {
                            const answer = response.answers.find(
                              a => a.questionId === question.questionId
                            );
                            return (
                              <tr key={question.questionId}>
                                <td>
                                  <strong>{question.questionText}</strong>
                                  <div className="mt-1">
                                    {getQuestionTypeBadge(question.questionType)}
                                  </div>
                                </td>
                                <td>
                                  {answer ? (
                                    renderAnswerValue(answer, question)
                                  ) : (
                                    <em className="text-muted">No answer provided</em>
                                  )}
                                </td>
                              </tr>
                            );
                          })}
                      </tbody>
                    </Table>
                  </Accordion.Body>
                </Accordion.Item>
              ))}
            </Accordion>
          </Card.Body>
        </Card>
      )}

      <Modal show={showModal} onHide={() => setShowModal(false)} centered>
        <Modal.Header closeButton>
          <Modal.Title>Confirm Delete</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          Are you sure you want to delete the custom form "{form.title}"?
          {form.responseCount > 0 && (
            <div className="alert alert-warning mt-3">
              <strong>Warning:</strong> This form has {form.responseCount} response(s) that will also be deleted.
            </div>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowModal(false)}>
            Cancel
          </Button>
          <Button variant="danger" onClick={confirmDelete} disabled={busy}>
            {busy ? 'Deleting...' : 'Delete'}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default CustomFormDetail;
