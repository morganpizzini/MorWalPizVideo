import React from 'react';
import { Container, Row, Col, Card } from 'react-bootstrap';

// Placeholder data - replace with actual data fetching
const posts = [
  { id: 1, title: 'Request video', description: 'Do we record you? You want some videos? Ask us!', imageUrl: 'https://placehold.co/600x400', linkUrl: '/request-video' },
  { id: 2, title: 'Request advertise', description: 'If you want to promote your company through our videos', imageUrl: 'https://placehold.co/600x400', linkUrl: '/request-ad' }
];

const HomePage: React.FC = () => {
  return (
    <Row xs={1} md={2} lg={3} className="g-4">
      {posts.map((post) => (
        <Col key={post.id}>
          {/* Wrap Card with an anchor tag */}
          <a href={post.linkUrl} style={{ textDecoration: 'none', color: 'inherit' }}>
            <Card style={{ minHeight: '330px' }}>
              <Card.Img variant="top" src={post.imageUrl} style={{ height: '200px', objectFit: 'cover' }} />
              <Card.Body>
                <Card.Title>{post.title}</Card.Title>
                <Card.Text>{post.description}</Card.Text>
              </Card.Body>
            </Card>
          </a>
        </Col>
      ))}
    </Row>
  );
};

export default HomePage;
