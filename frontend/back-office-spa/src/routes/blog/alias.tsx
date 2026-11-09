import { Navigate, useParams } from 'react-router';

export default function BlogPostAlias() {
  const { id } = useParams();
  return <Navigate replace to={`/blogposts/${encodeURIComponent(id ?? '')}/edit`} />;
}
