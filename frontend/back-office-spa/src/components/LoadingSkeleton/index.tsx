import React from 'react';

interface SkeletonBlockProps {
  className?: string;
}

function SkeletonBlock({ className = '' }: SkeletonBlockProps) {
  return <span className={`loading-skeleton__block ${className}`} aria-hidden="true" />;
}

export interface PageSkeletonProps {
  rows?: number;
}

export function PageSkeleton({ rows = 4 }: PageSkeletonProps): React.ReactElement {
  return (
    <div className="loading-skeleton loading-skeleton--page" role="status" aria-live="polite">
      <span className="visually-hidden">Page content is loading</span>
      <SkeletonBlock className="loading-skeleton__title" />
      <SkeletonBlock className="loading-skeleton__lead" />
      <div className="loading-skeleton__details">
        {Array.from({ length: rows }, (_, index) => (
          <SkeletonBlock key={index} className="loading-skeleton__line" />
        ))}
      </div>
    </div>
  );
}

export interface TableSkeletonProps {
  rows?: number;
  columns?: number;
}

export function TableSkeleton({ rows = 5, columns = 4 }: TableSkeletonProps): React.ReactElement {
  return (
    <div className="loading-skeleton loading-skeleton--table" role="status" aria-live="polite">
      <span className="visually-hidden">Table content is loading</span>
      <div className="loading-skeleton__table-row loading-skeleton__table-row--header">
        {Array.from({ length: columns }, (_, index) => (
          <SkeletonBlock key={index} className="loading-skeleton__cell" />
        ))}
      </div>
      {Array.from({ length: rows }, (_, rowIndex) => (
        <div className="loading-skeleton__table-row" key={rowIndex}>
          {Array.from({ length: columns }, (_, columnIndex) => (
            <SkeletonBlock key={columnIndex} className="loading-skeleton__cell" />
          ))}
        </div>
      ))}
    </div>
  );
}
