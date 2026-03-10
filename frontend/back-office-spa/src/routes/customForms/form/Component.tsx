import React, { useState, useEffect } from 'react';
import { Form, Button, Card, Row, Col, Badge } from 'react-bootstrap';
import { useFetcher, useNavigate, useLoaderData, useParams } from 'react-router';
import { 
  CustomForm, 
  QuestionType, 
  AnyQuestion,
  QuestionOption,
  OpenQuestion,
  MultipleChoiceQuestion,
  SingleChoiceQuestion
} from '@morwalpizvideo/models';
import { useToast } from '@components/ToastNotification/ToastContext';
import GenericErrorList from '@components/GenericErrorList';
import PageHeader from '@components/PageHeader';

interface QuestionFormData {
  questionId: string;
  questionText: string;
  questionType: QuestionType;
  isRequired: boolean;
  order: number;
  options?: QuestionOption[];
}

const CustomFormForm: React.FC = () => {
  const existingForm = useLoaderData() as CustomForm | null;
  const params = useParams();
  const isEditMode = !!params.id;
  
  const [title, setTitle] = useState(existingForm?.title || '');
  const [description, setDescription] = useState(existingForm?.description || '');
  const [url, setUrl] = useState(existingForm?.url || '');
  const [active, setActive] = useState(existingForm?.active ?? true);
  const [questions, setQuestions] = useState<QuestionFormData[]>([]);
  
  const fetcher = useFetcher();
  const navigate = useNavigate();
  const toast = useToast();

  const busy = fetcher.state !== 'idle';
  const errors = fetcher.data?.errors;

  useEffect(() => {
    if (existingForm) {
      const formQuestions: QuestionFormData[] = existingForm.questions.map(q => ({
        questionId: q.questionId,
        questionText: q.questionText,
        questionType: q.questionType,
        isRequired: q.isRequired,
        order: q.order,
        options: (q.questionType === QuestionType.MultipleChoice || q.questionType === QuestionType.SingleChoice)
          ? (q as MultipleChoiceQuestion | SingleChoiceQuestion).options
          : undefined
      }));
      setQuestions(formQuestions);
    }
  }, [existingForm]);

  const addQuestion = () => {
    const newQuestion: QuestionFormData = {
      questionId: `q_${Date.now()}`,
      questionText: '',
      questionType: QuestionType.Open,
      isRequired: false,
      order: questions.length,
      options: []
    };
    setQuestions([...questions, newQuestion]);
  };

  const removeQuestion = (index: number) => {
    const newQuestions = questions.filter((_, i) => i !== index);
    // Update order
    newQuestions.forEach((q, i) => q.order = i);
    setQuestions(newQuestions);
  };

  const updateQuestion = (index: number, field: keyof QuestionFormData, value: any) => {
    const newQuestions = [...questions];
    
    // If changing question type, handle options accordingly
    if (field === 'questionType') {
      const newType = value as QuestionType;
      if (newType === QuestionType.Open) {
        newQuestions[index].options = undefined;
      } else if (!newQuestions[index].options) {
        newQuestions[index].options = [];
      }
    }
    
    newQuestions[index] = { ...newQuestions[index], [field]: value };
    setQuestions(newQuestions);
  };

  const addOption = (questionIndex: number) => {
    const newQuestions = [...questions];
    if (!newQuestions[questionIndex].options) {
      newQuestions[questionIndex].options = [];
    }
    const options = newQuestions[questionIndex].options!;
    const newOption: QuestionOption = {
      optionId: `opt_${Date.now()}`,
      optionText: '',
      order: options.length
    };
    newQuestions[questionIndex].options = [...options, newOption];
    setQuestions(newQuestions);
  };

  const removeOption = (questionIndex: number, optionIndex: number) => {
    const newQuestions = [...questions];
    const options = newQuestions[questionIndex].options!;
    newQuestions[questionIndex].options = options.filter((_, i) => i !== optionIndex);
    // Update order
    newQuestions[questionIndex].options!.forEach((opt, i) => opt.order = i);
    setQuestions(newQuestions);
  };

  const updateOption = (questionIndex: number, optionIndex: number, value: string) => {
    const newQuestions = [...questions];
    const options = [...newQuestions[questionIndex].options!];
    options[optionIndex] = { ...options[optionIndex], optionText: value };
    newQuestions[questionIndex].options = options;
    setQuestions(newQuestions);
  };

  const moveQuestion = (index: number, direction: 'up' | 'down') => {
    if (
      (direction === 'up' && index === 0) ||
      (direction === 'down' && index === questions.length - 1)
    ) {
      return;
    }

    const newQuestions = [...questions];
    const targetIndex = direction === 'up' ? index - 1 : index + 1;
    [newQuestions[index], newQuestions[targetIndex]] = [newQuestions[targetIndex], newQuestions[index]];
    
    // Update order
    newQuestions.forEach((q, i) => q.order = i);
    setQuestions(newQuestions);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (questions.length === 0) {
      toast.show('Error', 'At least one question is required', { variant: 'danger' });
      return;
    }

    // Convert to API format
    const apiQuestions: AnyQuestion[] = questions.map(q => {
      const base = {
        questionId: q.questionId,
        questionText: q.questionText,
        questionType: q.questionType,
        isRequired: q.isRequired,
        order: q.order
      };

      if (q.questionType === QuestionType.Open) {
        return base as OpenQuestion;
      } else if (q.questionType === QuestionType.MultipleChoice) {
        return {
          ...base,
          options: q.options || []
        } as MultipleChoiceQuestion;
      } else {
        return {
          ...base,
          options: q.options || []
        } as SingleChoiceQuestion;
      }
    });

    const formData = new FormData();
    formData.append('title', title);
    formData.append('description', description);
    formData.append('url', url);
    formData.append('active', active.toString());
    formData.append('questions', JSON.stringify(apiQuestions));

    fetcher.submit(formData, { method: 'post' });
  };

  const getQuestionTypeName = (type: QuestionType): string => {
    switch (type) {
      case QuestionType.Open: return 'Open Text';
      case QuestionType.MultipleChoice: return 'Multiple Choice';
      case QuestionType.SingleChoice: return 'Single Choice';
      default: return 'Unknown';
    }
  };

  return (
    <>
      <PageHeader title={isEditMode ? 'Edit Custom Form' : 'Create Custom Form'} />
      <GenericErrorList errors={errors?.generics} />

      <fetcher.Form method="post" onSubmit={handleSubmit}>
        <Card className="mb-3">
          <Card.Header>
            <h5 className="mb-0">Form Details</h5>
          </Card.Header>
          <Card.Body>
            <Form.Group className="mb-3" controlId="title">
              <Form.Label>Title*</Form.Label>
              <Form.Control
                type="text"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Enter form title"
                isInvalid={!!errors?.fields?.title}
                required
              />
              {errors?.fields?.title && (
                <Form.Control.Feedback type="invalid">
                  {errors.fields.title}
                </Form.Control.Feedback>
              )}
            </Form.Group>

            <Form.Group className="mb-3" controlId="description">
              <Form.Label>Description</Form.Label>
              <Form.Control
                as="textarea"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Enter form description"
                rows={3}
              />
            </Form.Group>

            <Form.Group className="mb-3" controlId="url">
              <Form.Label>URL*</Form.Label>
              <Form.Control
                type="text"
                value={url}
                onChange={(e) => setUrl(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '-'))}
                placeholder="e.g., shooting-interest-survey"
                isInvalid={!!errors?.fields?.url}
                required
              />
              <Form.Text className="text-muted">
                URL-friendly path for accessing the form (lowercase, hyphens only)
              </Form.Text>
              {errors?.fields?.url && (
                <Form.Control.Feedback type="invalid">
                  {errors.fields.url}
                </Form.Control.Feedback>
              )}
            </Form.Group>

            <Form.Group className="mb-3" controlId="active">
              <Form.Check
                type="switch"
                label="Active (form accepts responses)"
                checked={active}
                onChange={(e) => setActive(e.target.checked)}
              />
              <Form.Text className="text-muted">
                When active, the form will be visible to users and accept responses
              </Form.Text>
            </Form.Group>
          </Card.Body>
        </Card>

        <Card className="mb-3">
          <Card.Header className="d-flex justify-content-between align-items-center">
            <h5 className="mb-0">Questions</h5>
            <Button variant="success" size="sm" onClick={addQuestion} type="button">
              ➕ Add Question
            </Button>
          </Card.Header>
          <Card.Body>
            {questions.length === 0 ? (
              <p className="text-muted text-center">No questions added yet. Click "Add Question" to get started.</p>
            ) : (
              questions.map((question, qIndex) => (
                <Card key={question.questionId} className="mb-3">
                  <Card.Header className="d-flex justify-content-between align-items-center bg-light">
                    <div>
                      <Badge bg="secondary">Question {qIndex + 1}</Badge>
                      <Badge bg="info" className="ms-2">{getQuestionTypeName(question.questionType)}</Badge>
                      {question.isRequired && <Badge bg="danger" className="ms-2">Required</Badge>}
                    </div>
                    <div>
                      <Button
                        variant="outline-secondary"
                        size="sm"
                        onClick={() => moveQuestion(qIndex, 'up')}
                        disabled={qIndex === 0}
                        type="button"
                        className="me-1"
                      >
                        ↑
                      </Button>
                      <Button
                        variant="outline-secondary"
                        size="sm"
                        onClick={() => moveQuestion(qIndex, 'down')}
                        disabled={qIndex === questions.length - 1}
                        type="button"
                        className="me-2"
                      >
                        ↓
                      </Button>
                      <Button
                        variant="outline-danger"
                        size="sm"
                        onClick={() => removeQuestion(qIndex)}
                        type="button"
                      >
                        🗑️
                      </Button>
                    </div>
                  </Card.Header>
                  <Card.Body>
                    <Row className="mb-3">
                      <Col md={8}>
                        <Form.Group controlId={`question-${qIndex}-text`}>
                          <Form.Label>Question Text*</Form.Label>
                          <Form.Control
                            type="text"
                            value={question.questionText}
                            onChange={(e) => updateQuestion(qIndex, 'questionText', e.target.value)}
                            placeholder="Enter question text"
                            required
                          />
                        </Form.Group>
                      </Col>
                      <Col md={4}>
                        <Form.Group controlId={`question-${qIndex}-type`}>
                          <Form.Label>Question Type*</Form.Label>
                          <Form.Select
                            value={question.questionType}
                            onChange={(e) => updateQuestion(qIndex, 'questionType', parseInt(e.target.value))}
                          >
                            <option value={QuestionType.Open}>Open Text</option>
                            <option value={QuestionType.SingleChoice}>Single Choice</option>
                            <option value={QuestionType.MultipleChoice}>Multiple Choice</option>
                          </Form.Select>
                        </Form.Group>
                      </Col>
                    </Row>

                    <Form.Group className="mb-3" controlId={`question-${qIndex}-required`}>
                      <Form.Check
                        type="checkbox"
                        label="Required question"
                        checked={question.isRequired}
                        onChange={(e) => updateQuestion(qIndex, 'isRequired', e.target.checked)}
                      />
                    </Form.Group>

                    {(question.questionType === QuestionType.SingleChoice || 
                      question.questionType === QuestionType.MultipleChoice) && (
                      <div>
                        <div className="d-flex justify-content-between align-items-center mb-2">
                          <Form.Label className="mb-0">Options*</Form.Label>
                          <Button
                            variant="outline-primary"
                            size="sm"
                            onClick={() => addOption(qIndex)}
                            type="button"
                          >
                            ➕ Add Option
                          </Button>
                        </div>
                        {(!question.options || question.options.length === 0) ? (
                          <p className="text-muted">No options added yet.</p>
                        ) : (
                          question.options.map((option, optIndex) => (
                            <div key={option.optionId} className="d-flex mb-2">
                              <Form.Control
                                type="text"
                                value={option.optionText}
                                onChange={(e) => updateOption(qIndex, optIndex, e.target.value)}
                                placeholder={`Option ${optIndex + 1}`}
                                required
                              />
                              <Button
                                variant="outline-danger"
                                size="sm"
                                onClick={() => removeOption(qIndex, optIndex)}
                                type="button"
                                className="ms-2"
                              >
                                🗑️
                              </Button>
                            </div>
                          ))
                        )}
                      </div>
                    )}
                  </Card.Body>
                </Card>
              ))
            )}
          </Card.Body>
        </Card>

        <div className="d-flex justify-content-end gap-2">
          <Button variant="secondary" onClick={() => navigate('/customforms')} disabled={busy} type="button">
            Cancel
          </Button>
          <Button type="submit" disabled={busy}>
            {busy ? (isEditMode ? 'Updating...' : 'Creating...') : (isEditMode ? 'Update' : 'Create')}
          </Button>
        </div>
      </fetcher.Form>
    </>
  );
};

export default CustomFormForm;
