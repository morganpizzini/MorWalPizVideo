import { Card, Col, Row } from 'react-bootstrap';

interface SkeletonCardProps {
  lines?: number;
}

function SkeletonLine({ width = '100%', height = '14px' }: { width?: string; height?: string }) {
  return (
    <div
      className="skeleton-line"
      style={{ width, height, borderRadius: 4, background: 'linear-gradient(90deg, #e0e0e0 25%, #f0f0f0 50%, #e0e0e0 75%)', backgroundSize: '200% 100%', animation: 'shimmer 1.4s infinite' }}
    />
  );
}

export function SkeletonCard({ lines = 3 }: SkeletonCardProps) {
  return (
    <Card className="h-100 shadow-sm" style={{ overflow: 'hidden' }}>
      <div style={{ height: '200px', background: '#e0e0e0', animation: 'shimmer 1.4s infinite', backgroundSize: '200% 100%', backgroundImage: 'linear-gradient(90deg, #e0e0e0 25%, #f0f0f0 50%, #e0e0e0 75%)' }} />
      <Card.Body className="d-flex flex-column gap-2">
        <SkeletonLine width="40%" height="20px" />
        <SkeletonLine width="70%" height="22px" />
        {Array.from({ length: lines }).map((_, i) => (
          <SkeletonLine key={i} width={i === lines - 1 ? '55%' : '90%'} />
        ))}
        <div className="mt-auto pt-2">
          <SkeletonLine height="36px" />
        </div>
      </Card.Body>
      <style>{`
        @keyframes shimmer {
          0% { background-position: 200% 0; }
          100% { background-position: -200% 0; }
        }
      `}</style>
    </Card>
  );
}

export function CompetitionsSkeletonGrid({ count = 6 }: { count?: number }) {
  return (
    <Row>
      {Array.from({ length: count }).map((_, i) => (
        <Col key={i} md={6} lg={4} className="mb-4">
          <SkeletonCard />
        </Col>
      ))}
    </Row>
  );
}
