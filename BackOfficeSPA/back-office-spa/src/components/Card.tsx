import React from 'react';
import { useNavigate } from 'react-router';
import { Card as BootstrapCard, Button } from 'react-bootstrap';

interface CardProps {
  title: string;
  subtitle?: string;
  content?: string;
  link: string;
  buttonText?: string;
}

const Card: React.FC<CardProps> = ({
  title,
  subtitle,
  content,
  link,
  buttonText = 'Go somewhere',
}) => {
    const navigate = useNavigate();
  return (
    <BootstrapCard style={{ width: '18rem' }}>
      <BootstrapCard.Body>
        <BootstrapCard.Title>{title}</BootstrapCard.Title>
        {subtitle && (
          <BootstrapCard.Subtitle className="mb-2 text-muted">{subtitle}</BootstrapCard.Subtitle>
        )}
              {content && <BootstrapCard.Text>{content}</BootstrapCard.Text>}
              <Button variant="primary" onClick={()=>navigate(link)}>
          {buttonText}
        </Button>
      </BootstrapCard.Body>
    </BootstrapCard>
  );
};

export default Card;
