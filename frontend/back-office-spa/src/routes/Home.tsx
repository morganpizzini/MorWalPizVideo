import React from 'react';
import Card from '../components/Card';
import { Container, Row, Col } from 'react-bootstrap';
import {
  Link,
  Folder,
  ExternalLink,
  Tv,
  Video,
  Image,
  Calendar,
  Settings
} from 'lucide-react';

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
              icon={Link}
              isSmall={true}
              gradientColors={['#FF6B6B', '#4ECDC4']}
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Categories Page"
              subtitle="Navigate to the categories page"
              content="Click the button below to go to the categories page."
              link="/categories"
              buttonText="Go to Categories"
              isSmall={true}
              icon={Folder}
              gradientColors={['#45B7D1', '#96CEB4']}
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Shortlinks Page"
              subtitle="Navigate to the shortlinks page"
              content="Click the button below to go to the shortlink page."
              link="/shortlinks"
              buttonText="Go to Shortlinks"
              icon={ExternalLink}
              isSmall={true}
              gradientColors={['#667eea', '#764ba2']}
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Channel Page"
              subtitle="Navigate to the channel page"
              content="Click the button below to go to the channel page."
              link="/channels"
              buttonText="Go to Channel"
              icon={Tv}
              isSmall={true}
              gradientColors={['#f093fb', '#f5576c']}
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Videos Page"
              subtitle="Gestione dei Video"
              content="Crea, importa o converti i tuoi video YouTube con facilità."
              link="/videos"
              buttonText="Gestisci Video"
              icon={Video}
              isSmall={true}
              gradientColors={['#43e97b', '#38f9d7']}
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Images Page"
              subtitle="Gestione delle Immagini"
              content="Carica e gestisci le immagini per i tuoi contenuti con facilità."
              link="/images"
              buttonText="Gestisci Immagini"
              icon={Image}
              isSmall={true}
              gradientColors={['#fa709a', '#fee140']}
            />
          </Col>          <Col md={4} className="g-4">
            <Card
              title="Calendar events"
              subtitle="Gestione degli eventi a calendario"
              content="Carica e gestisci gli eventi a calendario"
              link="/calendarEvents"
              buttonText="Gestisci Eventi"
              icon={Calendar}
              isSmall={true}
              gradientColors={['#a8edea', '#fed6e3']}
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Configurations"
              subtitle="Gestione delle configurazioni"
              content="Gestisci le configurazioni di sistema e dell'applicazione"
              link="/morwalpizconfigurations"
              buttonText="Gestisci Configurazioni"
              icon={Settings}
              isSmall={true}
              gradientColors={['#ffecd2', '#fcb69f']}
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Products"
              subtitle="Gestione dei prodotti"
              content="Gestisci i prodotti che appariranno nel frontend"
              link="/products"
              buttonText="Gestisci prodotti"
              icon={Settings}
              isSmall={true}
              gradientColors={['#ffecd2', '#fcb69f']}
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Product categories"
              subtitle="Gestione delle categorie prodotti"
              content="Gestisci le categorie prodotti che appariranno nel frontend"
              link="/productCategories"
              buttonText="Gestisci categorie prodotti"
              icon={Settings}
              isSmall={true}
              gradientColors={['#ffecd2', '#fcb69f']}
            />
          </Col>
          <Col md={4} className="g-4">
            <Card
              title="Sponsor"
              subtitle="Gestione degli sponsor"
              content="Gestisci gli sponsor dell'applicazione"
              link="/sponsors"
              buttonText="Gestisci Sponsor"
              icon={Settings}
              isSmall={true}
              gradientColors={['#ffecd2', '#fcb69f']}
            />
          </Col>
        </Row>
      </Container>
    </>
  );
};

export default Home;
