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
          <Col md={4} className="g-4">
            <Card
              title="Querylinks Page"
              subtitle="Navigate to the querylinks page"
              content="Click the button below to go to the querylinks page."
              link="/querylinks"
              buttonText="Go to Querylinks"
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Categories Page"
              subtitle="Navigate to the categories page"
              content="Click the button below to go to the categories page."
              link="/categories"
              buttonText="Go to Categories"
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Shortlinks Page"
              subtitle="Navigate to the shortlinks page"
              content="Click the button below to go to the shortlink page."
              link="/shortlinks"
              buttonText="Go to Shortlinks"
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Channel Page"
              subtitle="Navigate to the channel page"
              content="Click the button below to go to the channel page."
              link="/channels"
              buttonText="Go to Channel"
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Videos Page"
              subtitle="Gestione dei Video"
              content="Crea, importa o converti i tuoi video YouTube con facilità."
              link="/videos"
              buttonText="Gestisci Video"
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Images Page"
              subtitle="Gestione delle Immagini"
              content="Carica e gestisci le immagini per i tuoi contenuti con facilità."
              link="/images"
              buttonText="Gestisci Immagini"
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Calendar events"
              subtitle="Gestione degli eventi a calendario"
              content="Carica e gestisci gli eventi a calendario"
              link="/calendarEvents"
              buttonText="Gestisci Eventi"
            />
          </Col>
        </Row>
      </Container>
    </>
  );
};

export default Home;
