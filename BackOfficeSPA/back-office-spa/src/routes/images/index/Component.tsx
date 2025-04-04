import React from 'react';
import { Card, Row, Col, Button } from 'react-bootstrap';
import { Link } from 'react-router';
import PageHeader from '@components/PageHeader';

const ImagesHome: React.FC = () => {
  return (
    <>
      <PageHeader title="Images Management" />
      <Row className="g-4">
        <Col md={6}>
          <Card className="h-100">
            <Card.Body className="d-flex flex-column">
              <div className="text-center mb-3">
                
              </div>
              <Card.Title className="text-center mb-3">Upload Single Image</Card.Title>
              <Card.Text>
                Upload a single image to a match folder. Images will be automatically resized
                to appropriate dimensions while maintaining aspect ratio.
              </Card.Text>
              <div className="mt-auto d-flex justify-content-center">
                <Link to="/images/upload">
                  <Button variant="primary">Upload Image</Button>
                </Link>
              </div>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100">
            <Card.Body className="d-flex flex-column">
              <div className="text-center mb-3">
                
              </div>
              <Card.Title className="text-center mb-3">Upload Multiple Images</Card.Title>
              <Card.Text>
                Upload multiple images at once to a match folder. Batch upload and processing
                with automatic resizing for all selected images.
              </Card.Text>
              <div className="mt-auto d-flex justify-content-center">
                <Link to="/images/upload-multiple">
                  <Button variant="success">Upload Multiple Images</Button>
                </Link>
              </div>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </>
  );
};

export default ImagesHome;