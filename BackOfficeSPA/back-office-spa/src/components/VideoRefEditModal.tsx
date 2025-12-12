import React, { useState, useEffect } from 'react';
import { Modal, Button, Form, Badge } from 'react-bootstrap';
import { VideoRef, CategoryRef } from '../models/video/types';

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
      setCategories(videoRef.categories || []);
    }
  }, [videoRef]);

  // Handle category checkbox changes
  const handleCategoryChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const categoryId = e.target.value;
    if (e.target.checked) {
      const category = availableCategories.find(cat => cat.id === categoryId);
      if (category) {
        setCategories([...categories, category]);
      }
    } else {
      setCategories(categories.filter(cat => cat.id !== categoryId));
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
                  const isInAvailableList = availableCategories.some(availCat => availCat.id === cat.id);
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
                    key={cat.id}
                    type="checkbox"
                    id={`category-${cat.id}`}
                    label={cat.title}
                    value={cat.id}
                    checked={categories.some(selectedCat => selectedCat.id === cat.id)}
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
