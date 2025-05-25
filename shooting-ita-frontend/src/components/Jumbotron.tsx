import React from 'react';
import { Container } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';
import './Jumbotron.css';

interface JumbotronProps extends React.HTMLAttributes<HTMLDivElement> {
  backgroundImageUrl?: string;
}

const Jumbotron: React.FC<JumbotronProps> = (props) => {
  const navigate = useNavigate();

  const handleNavigateHome = () => {
    navigate('/');
  };

  const { backgroundImageUrl, className, style, ...restProps } = props;

  // Crea l'oggetto stile combinato
  const combinedStyle: React.CSSProperties = {
    ...style,
    cursor: 'pointer',
  };

  // Aggiungi la variabile CSS per l'immagine di sfondo se fornita
  if (backgroundImageUrl) {
    // Nota: TypeScript potrebbe lamentarsi per le custom properties,
    // si può usare un cast o estendere React.CSSProperties se necessario.
    (combinedStyle as any)['--jumbotron-bg-image'] = `url(${backgroundImageUrl})`;
  }

  return (
    <div
      {...restProps}
      className={`jumbotron-container ${className || ''}`}
      onClick={handleNavigateHome}
      style={combinedStyle} // Applica lo stile combinato con la variabile CSS
    >
      <Container className="jumbotron-content text-white">
        <h1 className="text-dark">Shooting ITA</h1>
        <p className="text-muted">Welcome to our platform where you can request videos and advertisements.</p>
      </Container>
    </div>
  );
};

export default Jumbotron;
