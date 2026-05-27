import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faStar as faStarSolid } from '@fortawesome/free-solid-svg-icons';
import { faStar as faStarRegular } from '@fortawesome/free-regular-svg-icons';

interface StarRatingProps {
  value: number;
  max?: number;
  onChange?: (value: number) => void;
  readOnly?: boolean;
  size?: 'sm' | 'md' | 'lg';
}

const sizeMap = { sm: '1rem', md: '1.5rem', lg: '2rem' } as const;

export function StarRating({ value, max = 5, onChange, readOnly = false, size = 'md' }: StarRatingProps) {
  const [hovered, setHovered] = useState<number | null>(null);

  const display = hovered ?? value;

  return (
    <span className="d-inline-flex gap-1" style={{ lineHeight: 1 }}>
      {Array.from({ length: max }, (_, i) => {
        const star = i + 1;
        const filled = star <= display;
        return (
          <span
            key={star}
            style={{
              fontSize: sizeMap[size],
              color: filled ? '#f5c518' : '#ccc',
              cursor: readOnly ? 'default' : 'pointer',
              transition: 'color 0.1s',
            }}
            onMouseEnter={() => !readOnly && setHovered(star)}
            onMouseLeave={() => !readOnly && setHovered(null)}
            onClick={() => !readOnly && onChange?.(star)}
            aria-label={`${star} stelle`}
            role={readOnly ? undefined : 'button'}
          >
            <FontAwesomeIcon icon={filled ? faStarSolid : faStarRegular} />
          </span>
        );
      })}
    </span>
  );
}
