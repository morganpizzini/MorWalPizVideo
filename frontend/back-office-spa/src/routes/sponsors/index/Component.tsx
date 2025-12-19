import React, { useState, useEffect, useMemo } from 'react';
import { useLoaderData, useFetcher, Link } from 'react-router';
import { Button, Modal } from 'react-bootstrap';
import { useToast } from '@components/ToastNotification/ToastContext';
import PageHeader from '@components/PageHeader';
import type { Sponsor } from '@morwalpizvideo/models';
import GenericErrorList from '@components/GenericErrorList';
import GenericTable from '@components/Table';
import { ColumnDef } from '@tanstack/react-table';

const SponsorsIndex: React.FC = () => {
    const { sponsors: entities } = useLoaderData<{ sponsors: Sponsor[] }>();
    const fetcher = useFetcher();
    const busy = fetcher.state !== 'idle';
    const errors = fetcher.data?.errors;
    const result =
        fetcher.data != undefined &&
            (fetcher.data.errors == undefined || fetcher.data.errors.length == 0)
            ? fetcher.data
            : null;
    const toast = useToast();
    const [showDeleteModal, setShowDeleteModal] = useState(false);
    const [selectedSponsor, setSelectedSponsor] = useState<Sponsor | null>(null);

    useEffect(() => {
        if (!result || busy) return;
        setShowDeleteModal(false);

        if (result.success) {
            toast.show('Success', 'Sponsor deleted successfully', { variant: 'success' });
        }
    }, [result, busy]);


    const handleDeleteClick = (sponsor: Sponsor) => {
        setSelectedSponsor(sponsor);
        setShowDeleteModal(true);
    };

    const handleDeleteConfirm = () => {
        if (!selectedSponsor) return;
        fetcher.submit(
            { sponsorId: selectedSponsor.id },
            {
                method: 'post',
                action: location.pathname,
            }
        );
    };

    // Column definitions
    const columns = useMemo<ColumnDef<Sponsor>[]>(
        () => [
            {
                accessorKey: 'imgSrc',
                header: 'Logo',
                cell: info => {
                    const imgSrc = info.getValue() as string;
                    return imgSrc ? (
                        <img
                            src={imgSrc}
                            alt="Sponsor logo"
                            style={{ height: '40px', objectFit: 'contain' }}
                        />
                    ) : null;
                },
            },
            {
                accessorKey: 'title',
                header: 'Title',
                cell: info => (
                    <Link to={`/sponsors/${info.row.original.id}/edit`}>
                        {info.getValue() as string}
                    </Link>
                ),
            },
            {
                accessorKey: 'url',
                header: 'Website',
                cell: info => {
                    const url = info.getValue() as string;
                    return url ? (
                        <a href={url} target="_blank" rel="noopener noreferrer">
                            Visit
                        </a>
                    ) : null;
                },
            },
            {
                id: 'actions',
                header: () => <div className="text-end">Actions</div>,
                cell: props => {
                    const sponsor = props.row.original;
                    return (
                        <div className="text-end">
                            <Link className="btn btn-link px-1" to={`/sponsors/${sponsor.id}/edit`}>
                                Edit
                            </Link>
                            <Button variant="link" className="px-1" onClick={() => handleDeleteClick(sponsor)}>
                                Delete
                            </Button>
                        </div>
                    );
                },
            },
        ],
        []
    );

    return (
        <>
            <PageHeader title="Sponsors" createLink="./create" />
            <GenericErrorList errors={errors?.generics} />

            <GenericTable
                data={entities}
                columns={columns}
                pageSize={10}
                searchPlaceholder="Search sponsors..."
                emptyMessage="No sponsors found"
            />

            <Modal show={showDeleteModal} onHide={() => setShowDeleteModal(false)}>
                <Modal.Header closeButton>
                    <Modal.Title>Confirm Delete</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    Are you sure you want to delete the sponsor "{selectedSponsor?.title}"?
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" onClick={() => setShowDeleteModal(false)}>
                        Cancel
                    </Button>
                    <Button variant="danger" onClick={handleDeleteConfirm}>
                        Delete
                    </Button>
                </Modal.Footer>
            </Modal>
        </>
    );
};

export default SponsorsIndex;
