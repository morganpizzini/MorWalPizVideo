import React, { useState, useEffect } from 'react';
import { Modal, Button, Form, Badge } from 'react-bootstrap';
import { VideoRef, CategoryRef } from '../models/video/types';

type CategoryWithFallbackId = CategoryRef & { categoryId?: string };

const getCategoryId = (category: CategoryWithFallbackId): string =>
  category.id ?? category.categoryId ?? '';

interface VideoRefEditModalProps {
  show: boolean;
  videoRef: VideoRef | null;
  onHide: () => void;
  onSave: (updatedVideoRef: VideoRef) => void;
  availableCategories?: CategoryRef[];
}

const VideoRefEditModal: React.FC<VideoRefEditModalProps> = ({
  show,
  videoRef,
  onHide,
  onSave,
  availableCategories = []
}) => {
  const [categories, setCategories] = useState<CategoryRef[]>([]);

  useEffect(() => {
    if (videoRef) {
      const normalizedCategories = (videoRef.categories as CategoryWithFallbackId[]).map(category => ({
        id: getCategoryId(category),
        title: category.title,
      }));
      setCategories(normalizedCategories.filter(category => category.id.length > 0));
    }
  }, [videoRef]);

  // Handle category checkbox changes
  const handleCategoryChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const categoryId = e.target.value;
    if (e.target.checked) {
      const category = availableCategories.find(cat => getCategoryId(cat as CategoryWithFallbackId) === categoryId);
      if (category) {
        setCategories([...categories, { id: getCategoryId(category as CategoryWithFallbackId), title: category.title }]);
      }
    } else {
      setCategories(categories.filter(cat => getCategoryId(cat as CategoryWithFallbackId) !== categoryId));
    }
  };

  const handleSave = () => {
    if (videoRef) {
      const updatedVideoRef: VideoRef = {
        ...videoRef,
        categories
      };
      onSave(updatedVideoRef);
    }
  };

  const handleClose = () => {
    onHide();
  };

  if (!videoRef) return null;

  return (
    <Modal show={show} onHide={handleClose} size="lg">
      <Modal.Header closeButton>
        <Modal.Title>Edit Video Reference</Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <Form>
          <Form.Group className="mb-3">
            <Form.Label>YouTube ID</Form.Label>
            <Form.Control
              type="text"
              value={videoRef.youtubeId}
              disabled
              readOnly
            />
            <Form.Text className="text-muted">
              YouTube ID cannot be changed
            </Form.Text>
          </Form.Group>

          <Form.Group className="mb-3">
            <Form.Label>
              Categories <span className="text-danger">*</span>
            </Form.Label>

            {/* Display selected categories as badges */}
            <div className="mb-2 d-flex gap-1 flex-wrap">
              {categories.length > 0 ? (
                categories.map((cat, idx) => {
                  const selectedCategoryId = getCategoryId(cat as CategoryWithFallbackId);
                  const isInAvailableList = availableCategories.some(availCat => getCategoryId(availCat as CategoryWithFallbackId) === selectedCategoryId);
                  return (
                    <Badge
                      key={idx}
                      bg={isInAvailableList ? "secondary" : "warning"}
                      title={isInAvailableList ? undefined : "Category not found in available list"}
                    >
                      {cat.title}
                    </Badge>
                  );
                })
              ) : (
                <span className="text-muted small">No categories selected</span>
              )}
            </div>

            {/* Category checkboxes */}
            <div className="border rounded p-3">
              {availableCategories.length > 0 ? (
                availableCategories.map(cat => (
                  <Form.Check
                    key={getCategoryId(cat as CategoryWithFallbackId)}
                    type="checkbox"
                    id={`category-${getCategoryId(cat as CategoryWithFallbackId)}`}
                    label={cat.title}
                    value={getCategoryId(cat as CategoryWithFallbackId)}
                    checked={categories.some(selectedCat => getCategoryId(selectedCat as CategoryWithFallbackId) === getCategoryId(cat as CategoryWithFallbackId))}
                    onChange={handleCategoryChange}
                    className="mb-2"
                  />
                ))
              ) : (
                <p className="text-muted mb-0">No categories available</p>
              )}
            </div>
          </Form.Group>
        </Form>
      </Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" onClick={handleClose}>
          Cancel
        </Button>
        <Button variant="primary" onClick={handleSave}>
          Save Changes
        </Button>
      </Modal.Footer>
    </Modal>
  );
};

export default VideoRefEditModal;
