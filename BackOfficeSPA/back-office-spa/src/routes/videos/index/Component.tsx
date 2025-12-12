import React from 'react';
import { useLoaderData } from 'react-router';
import Card from '@components/Card';
import PageHeader from '@components/PageHeader';
import VideoList from '@components/VideoList';
import { Row, Col } from 'react-bootstrap';
import { Match } from '@models/video/types';
import {
  Download,
  Languages,
  Plus,
  Paperclip,
  RefreshCw,
  ImageIcon,
  ExternalLink
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
    },
    {
      id: 'root',
      title: 'Crea Root Video',
      path: '/videos/create-root',
      icon: Plus,
      description: 'Crea un nuovo video root con titolo, descrizione e categoria',
      gradientColors: ['#43e97b', '#38f9d7'] as [string, string],
    },
    {
      id: 'sub',
      title: 'Crea Sub-Video',
      path: '/videos/create-sub-video',
      icon: Paperclip,
      description: 'Associa un video esistente a un video root (match)',
      gradientColors: ['#fa709a', '#fee140'] as [string, string],
    },
    {
      id: 'convert',
      title: 'Converti in Root',
      path: '/videos/convert-to-root',
      icon: RefreshCw,
      description: 'Converti un video esistente in un video root',
      gradientColors: ['#a8edea', '#fed6e3'] as [string, string],
    },
    {
      id: 'thumbnail',
      title: 'Cambia Thumbnail',
      path: '/videos/swap-thumbnail',
      icon: ImageIcon,
      description: 'Sostituisci la thumbnail di un video root con quella di un altro video',
      gradientColors: ['#ffecd2', '#fcb69f'] as [string, string],
    },
    {
      id: 'youtube-links',
      title: 'YouTube Video Links',
      path: '/videos/youtube-links',
      icon: ExternalLink,
      description: 'Gestisci i link video YouTube per creare linktree dei match',
      gradientColors: ['#FF6B6B', '#4ECDC4'] as [string, string],
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
