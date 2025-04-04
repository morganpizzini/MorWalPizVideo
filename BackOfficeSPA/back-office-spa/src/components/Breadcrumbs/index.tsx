import React, { useEffect, useState } from 'react';
import { Link, useLocation, useMatches } from 'react-router';
import { Breadcrumb, Button } from 'react-bootstrap';
import './index.css'; // Assuming you have a global CSS file for styles
interface BreadcrumbsProps {
  // Optional custom labels for path segments
  customLabels?: Record<string, string>;
  // Optional toggleSidebar function
  toggleSidebar?: () => void;
}

const Breadcrumbs: React.FC<BreadcrumbsProps> = ({ customLabels = {}, toggleSidebar }) => {
  const location = useLocation();
  const matches = useMatches();
  const [entity, setEntity] = useState<Record<string, string> | undefined>(undefined);

  useEffect(() => {
    const lastMatchData = matches[matches.length - 1]?.data;
    setEntity(
      lastMatchData && typeof lastMatchData === 'object' && !Array.isArray(lastMatchData)
        ? (lastMatchData as Record<string, string>)
        : undefined
    );
    return () => {
      setEntity(undefined);
    };
  }, [matches]);

  // Skip rendering breadcrumbs on home page
  if (location.pathname === '/') {
    return <></>;
  }

  // Split the path into segments
  const pathSegments = location.pathname.split('/').filter(Boolean);

  // Define default labels for known routes
  const defaultLabels: Record<string, string> = {
    categories: 'Categories',
    querylinks: 'Query Links',
    shortlinks: 'Short Links',
    channels: 'Channels',
    videos: 'Videos',
    create: 'Create',
    edit: 'Edit',
  };

  // Combine default labels with custom labels
  const labels = { ...defaultLabels, ...customLabels };

  // Function to get entity name
  const getEntityName = (segment: string, type: string): string => {
    if (!entity) return segment;

    if ((type === 'querylinks' || type === 'categories') && entity.title) {
      return entity.title;
    } else if (type === 'shortlinks' && entity.videoId) {
      return entity.videoId;
    } else if (type === 'channels' && entity.channelName) {
      return entity.channelName;
    }

    return segment;
  };

  return (
    <div className="d-flex align-items-center mt-3 mb-3">
      {toggleSidebar && (
        <Button
          variant="outline-secondary"
          className="me-2"
          onClick={toggleSidebar}
          aria-label="Toggle sidebar"
        >
          ☰
        </Button>
      )}
      <Breadcrumb className="mb-0 flex-grow-1">
        <Breadcrumb.Item linkAs={Link} linkProps={{ to: '/' }}>
          Home
        </Breadcrumb.Item>

        {pathSegments.map((segment, index) => {
          const isLast = index === pathSegments.length - 1;

          // Build the URL for this breadcrumb
          const url = `/${pathSegments.slice(0, index + 1).join('/')}`;

          // Determine if this is a dynamic segment (like an ID)
          const isId = segment !== 'create' && segment !== 'edit' && !labels[segment];

          // Determine the previous segment type (for entity name lookup)
          const parentType = index > 0 ? pathSegments[0] : '';

          // Get the appropriate label
          const label = isId ? getEntityName(segment, parentType) : labels[segment] || segment;

          return (
            <Breadcrumb.Item
              key={url}
              active={isLast}
              linkAs={isLast ? undefined : Link}
              linkProps={isLast ? undefined : { to: url }}
            >
              {label}
            </Breadcrumb.Item>
          );
        })}
      </Breadcrumb>
    </div>
  );
};

export default Breadcrumbs;
