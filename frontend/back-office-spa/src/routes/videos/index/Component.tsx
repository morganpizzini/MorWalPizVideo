import React from 'react';
import { useLoaderData } from 'react-router';
import Card from '@components/Card';
import PageHeader from '@components/PageHeader';
import VideoList from '@components/VideoList';
import { Row, Col } from 'react-bootstrap';
import { Match } from '@morwalpizvideo/models';
import {
  Download,
  Languages
} from 'lucide-react';

const Component: React.FC = () => {
  const { matches } = useLoaderData() as { matches: Match[] };
  const features = [
    {
      id: 'import',
      title: 'Importa Video',
      path: '/videos/import',
      icon: Download,
      description: 'Importa un video YouTube nella piattaforma MorWalPiz',
      gradientColors: ['#667eea', '#764ba2'] as [string, string],
    },
    {
      id: 'translate',
      title: 'Traduci Video',
      path: '/videos/translate',
      icon: Languages,
      description: 'Traduci i metadati di uno o più video shorts',
      gradientColors: ['#f093fb', '#f5576c'] as [string, string],
    }
  ];

  return (
    <>
      <PageHeader title="Gestione Video" />

      <p className="lead text-muted mb-4">
        Utilizza questa dashboard per gestire i video della piattaforma MorWalPiz. Puoi importare
        contenuti e tradurre rapidamente i metadati dei video.
      </p>

      <Row xs={1} md={3} lg={4} className="g-4">
        {features.map(feature => (
          <Col key={feature.id}>
            <Card
              title={feature.title}
              content={feature.description}
              link={feature.path}
              buttonText={`Vai a ${feature.title}`}
              icon={feature.icon}
              isSmall={true}
              gradientColors={feature.gradientColors}
            />
          </Col>
        ))}
      </Row>

      <VideoList matches={matches} />
    </>
  );
};

export default Component;
