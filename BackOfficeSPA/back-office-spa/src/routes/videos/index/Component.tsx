import React from 'react';
import Card from '@components/Card';
import PageHeader from '@components/PageHeader';
import { Row, Col } from 'react-bootstrap';

const Component: React.FC = () => {
  const features = [
    {
      id: 'import',
      title: 'Importa Video',
      path: '/videos/import',
      icon: '📥',
      description: 'Importa un video YouTube nella piattaforma MorWalPiz',
    },
    {
      id: 'translate',
      title: 'Traduci Video',
      path: '/videos/translate',
      icon: '🔄',
      description: 'Traduci i metadati di uno o più video shorts',
    },
    {
      id: 'root',
      title: 'Crea Root Video',
      path: '/videos/create-root',
      icon: '➕',
      description: 'Crea un nuovo video root con titolo, descrizione e categoria',
    },
    {
      id: 'sub',
      title: 'Crea Sub-Video',
      path: '/videos/create-sub-video',
      icon: '📎',
      description: 'Associa un video esistente a un video root (match)',
    },
    {
      id: 'convert',
      title: 'Converti in Root',
      path: '/videos/convert-to-root',
      icon: '🔄',
      description: 'Converti un video esistente in un video root',
    },
    {
      id: 'thumbnail',
      title: 'Cambia Thumbnail',
      path: '/videos/swap-thumbnail',
      icon: '🖼️',
      description: 'Sostituisci la thumbnail di un video root con quella di un altro video',
    }
  ];

  return (
    <>
      <PageHeader title="Gestione Video" />

      <p className="lead text-muted mb-4">
        Utilizza questa dashboard per gestire i video della piattaforma MorWalPiz. Puoi importare
        video, creare video root, associare sub-video, cambiare thumbnail e altro.
      </p>

      <Row xs={1} md={3} lg={4} className="g-4">
        {features.map(feature => (
          <Col key={feature.id}>
            <Card
              title={`${feature.icon} ${feature.title}`}
              content={feature.description}
              link={feature.path}
              buttonText={`Vai a ${feature.title}`}
            />
          </Col>
        ))}
      </Row>
    </>
  );
};

export default Component;
