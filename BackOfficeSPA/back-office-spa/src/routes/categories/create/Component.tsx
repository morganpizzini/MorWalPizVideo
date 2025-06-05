import React, { useState, useEffect } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useNavigate, useFetcher } from 'react-router';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';

const CreateCategory: React.FC = () => {
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [showModal, setShowModal] = useState(false);
    const navigate = useNavigate();
    const toast = useToast();

    const fetcher = useFetcher();
    const busy = fetcher.state !== 'idle';
    const errors = fetcher.data?.errors;
    const result =
        fetcher.data != undefined &&
            (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
            ? fetcher.data
            : null;

    useEffect(() => {
        if (!result || busy) return;
        setShowModal(false);
        if (result.success) {
            toast.show('Success', 'Category created successfully', { variant: 'success' });
            navigate('..');
        }
    }, [result, busy, navigate]);

    const isDisabled = () => title.length === 0 || description.length === 0 || busy;

    const handleSubmit = (event: React.FormEvent) => {
        event.preventDefault();
        setShowModal(true);
    };

    const confirmCreate = () => {
        fetcher.submit(
            {
                title,
                description,
            },
            {
                method: 'post',
                action: location.pathname,
            }
        );
    };

    return (
        <>
            <PageHeader title="Create Category" />
            <GenericErrorList errors={errors?.generics} />
            <Form onSubmit={handleSubmit}>
                <Form.Group controlId="formTitle">
                    <Form.Label>
                        Title <span className="text-danger">*</span>
                    </Form.Label>
                    <Form.Control
                        autoComplete="off"
                        type="text" value={title} onChange={e => setTitle(e.target.value)} />
                    <FieldError error={errors?.title} />
                </Form.Group>
                <Form.Group controlId="formDescription">
                    <Form.Label>
                        Description <span className="text-danger">*</span>
                    </Form.Label>
                    <Form.Control
                        autoComplete="off"
                        type="text"
                        value={description}
                        onChange={e => setDescription(e.target.value)}
                    />
                    <FieldError error={errors?.description} />
                </Form.Group>
                <Button variant="success" disabled={isDisabled()} type="submit" className="mt-2">
                    Create
                </Button>
            </Form>

            <Modal show={showModal} onHide={() => setShowModal(false)}>
                <Modal.Header closeButton>
                    <Modal.Title>Confirm Create</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    <p>Are you sure you want to create the following category?</p>
                    <p>
                        <strong>Title:</strong> {title}
                    </p>
                    <p>
                        <strong>Description:</strong> {description}
                    </p>
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" onClick={() => setShowModal(false)}>
                        Cancel
                    </Button>
                    <Button variant="success" onClick={confirmCreate} data-testid="create-modal-confirm">
                        Create
                    </Button>
                </Modal.Footer>
            </Modal>
        </>
    );
};

export default CreateCategory;
