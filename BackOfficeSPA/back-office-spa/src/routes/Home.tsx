import React from 'react';
import Card from '../components/Card';
import { Container, Row, Col } from 'react-bootstrap';

const Home: React.FC = () => {
  return (
    <>
      <h1>BackOffice MorWalPiz</h1>
      <p className="text-muted">Seleziona un opzione per proseguire</p>
      <hr />
      <h3>Gestione youtube</h3>
      <Container>
        <Row>
          <Col md={4}>
            <Card
              title="Querylinks Page"
              subtitle="Navigate to the querylinks page"
              content="Click the button below to go to the querylinks page."
              link="/querylinks"
              buttonText="Go to Querylinks"
            />
          </Col>
          <Col md={4}>
            <Card
              title="Shortlinks Page"
              subtitle="Navigate to the shortlinks page"
              content="Click the button below to go to the shortlink page."
              link="/shortlinks"
              buttonText="Go to Shortlinks"
            />
          </Col>
          <Col md={4}>
            <Card
              title="Channel Page"
              subtitle="Navigate to the channel page"
              content="Click the button below to go to the channel page."
              link="/channels"
              buttonText="Go to Channel"
            />
          </Col>
        </Row>
      </Container>
    </>
  );
};

export default Home;
