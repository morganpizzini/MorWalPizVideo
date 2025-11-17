import React, { useEffect, useState } from 'react';
import { useFetcher, useNavigate, useLoaderData } from 'react-router';
import { Form, Button, Modal } from 'react-bootstrap';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import { QueryLink } from '@/models';
import PageHeader from '@components/PageHeader';

const EditQueryLink: React.FC = () => {
    const [model, setModel] = useState<QueryLink | null>(null);
    const [showModal, setShowModal] = useState(false);
    const navigate = useNavigate();
    const toast = useToast();

    const entity = useLoaderData<QueryLink>();

    useEffect(() => {
        setModel(entity);
    }, [entity]);

    const fetcher = useFetcher();

    const busy = fetcher.state !== 'idle';
    const errors = fetcher.data?.errors;
    const result =
        fetcher.data != undefined &&
            (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
            ? fetcher.data
            : null;

    useEffect(() => {
        if (!result) return;
        setShowModal(false);

        if (result.success) {
            toast.show('Success', 'Query link updated successfully', { variant: 'success' });
            navigate('..');
        }
    }, [result, navigate]);

    const handleSubmit = (event: React.FormEvent) => {
        event.preventDefault();
        setShowModal(true);
    };

    const confirmEdit = () => {
        fetcher.submit(
            { ...model },
            {
                method: 'post',
                action: location.pathname,
            }
        );
    };

    const isDisabled = () =>
        !model || model.title.length === 0 || model.value.length === 0 || busy;

    if (!model) {
        return <div>Loading...</div>;
    }

    return (
        <>
            <PageHeader title="Edit Query Link" />
            <GenericErrorList errors={errors?.generics} />
            <Form onSubmit={handleSubmit}>
                <Form.Group controlId="formTitle">
                    <Form.Label>
                        Title <span className="text-danger">*</span>
                    </Form.Label>
                    <Form.Control
                        type="text"
                        value={model.title}
                        onChange={e => setModel({ ...model, title: e.target.value })}
                    />
                    <FieldError error={errors?.title} />
                </Form.Group>
                <Form.Group controlId="formValue">
                    <Form.Label>
                        Value <span className="text-danger">*</span>
                    </Form.Label>
                    <Form.Control
                        type="text"
                        value={model.value}
                        onChange={e => setModel({ ...model, value: e.target.value })}
                    />
                    <FieldError error={errors?.value} />
                </Form.Group>
                <Button variant="success" disabled={isDisabled()} type="submit" className="mt-2">
                    Save Changes
                </Button>
            </Form>

            <Modal show={showModal} onHide={() => setShowModal(false)}>
                <Modal.Header closeButton>
                    <Modal.Title>Confirm Edit</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    <p>Are you sure you want to save the following changes?</p>
                    <p>
                        <strong>Title:</strong> {model.title}{' '}
                        {model.title != entity.title && (
                            <>
                                (<s>{entity.title}</s>)
                            </>
                        )}
                    </p>
                    <p>
                        <strong>Value:</strong> {model.value}{' '}
                        {model.value != entity.value && (
                            <>
                                (<s>{entity.value}</s>)
                            </>
                        )}
                    </p>
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" onClick={() => setShowModal(false)}>
                        Cancel
                    </Button>
                    <Button variant="primary" onClick={confirmEdit} data-testid="edit-modal-confirm">
                        Save Changes
                    </Button>
                </Modal.Footer>
            </Modal>
        </>
    );
};

export default EditQueryLink;
