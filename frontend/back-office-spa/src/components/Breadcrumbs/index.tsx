import React, { Suspense } from 'react';
import { Await, Link, useLocation, useMatches } from 'react-router';
import type { BlogPostAdmin } from '@morwalpizvideo/models';
import { Breadcrumb, Button } from 'react-bootstrap';
import './index.css'; // Assuming you have a global CSS file for styles
interface BreadcrumbsProps {
  // Optional custom labels for path segments
  customLabels?: Record<string, string>;
  // Optional toggleSidebar function
  toggleSidebar?: () => void;
}

type BlogBreadcrumb = (post: BlogPostAdmin | undefined) => string;

interface BlogBreadcrumbHandle {
  breadcrumb: BlogBreadcrumb;
}

function isBlogBreadcrumbHandle(handle: unknown): handle is BlogBreadcrumbHandle {
  return (
    typeof handle === 'object' &&
    handle !== null &&
    'breadcrumb' in handle &&
    typeof handle.breadcrumb === 'function'
  );
}

function getBlogPostPromise(data: unknown): Promise<BlogPostAdmin> | undefined {
  if (typeof data !== 'object' || data === null || !('data' in data)) return undefined;
  const resolvedData = data.data;
  return resolvedData instanceof Promise ? resolvedData : undefined;
}

const Breadcrumbs: React.FC<BreadcrumbsProps> = ({ customLabels = {}, toggleSidebar }) => {
  const location = useLocation();
  const matches = useMatches();

  // Skip rendering breadcrumbs on home page
  if (location.pathname === '/') {
    return <></>;
  }

  // Split the path into segments
  const pathSegments = location.pathname.split('/').filter(Boolean);

  // Define default labels for known routes
  const defaultLabels: Record<string, string> = {
    categories: 'Categories',
    products: 'Products',
    querylinks: 'Query Links',
    shortlinks: 'Short Links',
    channels: 'Channels',
    videos: 'Videos',
    blogposts: 'Blog articles',
    'dashboard-actions': 'Attività rapide',
    rbac: 'RBAC',
    create: 'Create',
    edit: 'Edit',
  };

  // Combine default labels with custom labels
  const labels = { ...defaultLabels, ...customLabels };

  const blogMatch = matches.find(match => isBlogBreadcrumbHandle(match.handle));
  const blogHandle =
    blogMatch && isBlogBreadcrumbHandle(blogMatch.handle) ? blogMatch.handle : undefined;
  const lastMatchData = matches[matches.length - 1]?.data;
  const flatEntity =
    lastMatchData && typeof lastMatchData === 'object' && !Array.isArray(lastMatchData)
      ? (lastMatchData as Record<string, string>)
      : undefined;
  const getEntityName = (segment: string, index: number): React.ReactNode => {
    if (
      index === 1 &&
      segment !== 'create' &&
      segment !== 'edit' &&
      blogMatch &&
      blogHandle
    ) {
      const data = getBlogPostPromise(blogMatch.data);
      if (data) {
        const breadcrumb = blogHandle.breadcrumb;
        return (
          <Suspense fallback="Article">
            <Await resolve={data} errorElement="Article">
              {(post: BlogPostAdmin) => breadcrumb(post)}
            </Await>
          </Suspense>
        );
      }
    }
    return flatEntity?.breadcrumbIdentifier || segment;
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

          // Get the appropriate label
          const label = isId ? getEntityName(segment, index) : labels[segment] || segment;

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
