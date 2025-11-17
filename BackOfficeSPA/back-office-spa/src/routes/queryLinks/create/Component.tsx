import React, { useState, useEffect } from 'react';
import { Form, Button, Modal } from 'react-bootstrap';
import { useNavigate, useFetcher } from 'react-router';
import GenericErrorList from '@components/GenericErrorList';
import FieldError from '@components/FieldError';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';

const CreateQueryLink: React.FC = () => {
    const [title, setTitle] = useState('');
    const [value, setValue] = useState('');
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
        if (!result) return;
        setShowModal(false);

        if (result.success) {
            toast.show('Success', 'Query link created successfully', { variant: 'success' });
            navigate('..');
        }
    }, [result, navigate]);

    const isDisabled = () => title.length === 0 || value.length === 0 || busy;

    const handleSubmit = (event: React.FormEvent) => {
        event.preventDefault();
        setShowModal(true);
    };

    const confirmCreate = () => {
        fetcher.submit(
            {
                title,
                value,
            },
            {
                method: 'post',
                action: location.pathname,
            }
        );
    };

    return (
        <>
            <PageHeader title="Create Query Link" />
            <GenericErrorList errors={errors?.generics} />
            <Form onSubmit={handleSubmit}>
                <Form.Group controlId="formTitle">
                    <Form.Label>
                        Title <span className="text-danger">*</span>
                    </Form.Label>
                    <Form.Control type="text" value={title} onChange={e => setTitle(e.target.value)} />
                    <FieldError error={errors?.title} />
                </Form.Group>
                <Form.Group controlId="formDescription">
                    <Form.Label>
                        Value <span className="text-danger">*</span>
                    </Form.Label>
                    <Form.Control
                        type="text"
                        value={value}
                        onChange={e => setValue(e.target.value)}
                    />
                    <FieldError error={errors?.value} />
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
                    <p>Are you sure you want to create the following query link?</p>
                    <p>
                        <strong>Title:</strong> {title}
                    </p>
                    <p>
                        <strong>Description:</strong> {value}
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

export default CreateQueryLink;
