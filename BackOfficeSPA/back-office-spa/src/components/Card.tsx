import React from 'react';
import { useNavigate } from 'react-router';
import { LucideIcon } from 'lucide-react';
import './Card.css';

interface CardProps {
  title: string;
  subtitle?: string;
  content?: string;
  link: string;
  buttonText?: string;
  icon?: LucideIcon;
  gradientColors?: [string, string];
  isSmall?: boolean;
}

const Card: React.FC<CardProps> = ({
  title,
  subtitle,
  content,
  link,
  buttonText = 'Go somewhere',
  icon: Icon,
  gradientColors = ['red', 'blue'],
  isSmall = false,
}) => {
  const navigate = useNavigate();

  const handleClick = () => {
    navigate(link);
  };

  const gradientId = `gradient-${title.replace(/\s+/g, '-').toLowerCase()}`;

  return (
    <>
      {/* SVG gradient definitions */}
      <svg className="gradient-defs" width="0" height="0">
        <defs>
          <linearGradient id={gradientId} x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor={gradientColors[0]} />
            <stop offset="100%" stopColor={gradientColors[1]} />
          </linearGradient>
        </defs>
      </svg>
      
      <div 
        className={`card ${isSmall ? 'card-small' : ''}`}
        onClick={handleClick}
        style={{ '--grad': gradientColors.join(', ') } as React.CSSProperties}
      >
        <div className="title">{title}</div>
        
        {Icon && (
          <div className="icon">
            <Icon stroke={`url(#${gradientId})`} strokeWidth={1.5} />
          </div>
        )}
        
        <div className="content">
          {subtitle && <h6 className="mb-2 text-muted">{subtitle}</h6>}
          {content && <p>{content}</p>}
        </div>
      </div>
    </>
  );
};

export default Card;
