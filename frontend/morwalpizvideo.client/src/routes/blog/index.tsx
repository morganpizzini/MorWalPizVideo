import { Link, useLoaderData, type LoaderFunctionArgs } from 'react-router';
import type { BlogPage, BlogPostPublic } from '@morwalpizvideo/models';
import { ApiResponseError, getPublicBlog, getPublicBlogPost } from '@morwalpizvideo/services';
import { BlogRenderer } from '@morwalpiz/layout';
import { Helmet } from 'react-helmet-async';
import SEO from '../../utils/seo';
import './style.scss';

export async function blogLoader({ request }: LoaderFunctionArgs): Promise<BlogPage> {
  const page = Number(new URL(request.url).searchParams.get('page') ?? 1);
  if (!Number.isInteger(page) || page < 1 || page > 10000)
    throw new Response('Invalid page', { status: 400 });
  return getPublicBlog(page);
}

export async function articleLoader({ params }: LoaderFunctionArgs): Promise<BlogPostPublic> {
  try {
    return await getPublicBlogPost(params.slug ?? '');
  } catch (error) {
    if (error instanceof ApiResponseError)
      throw new Response('Article unavailable', { status: error.status });
    throw error;
  }
}

export function BlogList() {
  const page = useLoaderData() as BlogPage;
  return (
    <section className="blog-surface p-3 p-md-4 bg-white">
      <SEO title="Blog | MorWalPiz" description="Articoli MorWalPiz" type="website" />
      <h1 className="page-title">Blog</h1>
      {!page.items.length && <p role="status">Nessun articolo pubblicato.</p>}
      <div className="row g-4 mt-1">
        {page.items.map((post) => (
          <article key={post.slug} className="col-12 col-md-6">
            {post.coverUrl && (
              <Link to={`/blog/${post.slug}`} tabIndex={-1} aria-hidden="true">
                <img
                  src={post.coverUrl}
                  alt={post.coverAlt}
                  className="blog-cover img-fluid"
                  loading="lazy"
                />
              </Link>
            )}
            <h2 className="h4 mt-3">
              <Link to={`/blog/${post.slug}`}>{post.title}</Link>
            </h2>
            <p className="text-secondary mb-2">
              {post.author}
              {post.author && ' · '}
              <time dateTime={post.publishedAt}>{post.publishedAt.slice(0, 10)}</time>
            </p>
            <p>{post.summary}</p>
          </article>
        ))}
      </div>
      <nav aria-label="Pagine blog" className="d-flex justify-content-between mt-4">
        {page.page > 1 ? (
          <Link to={`?page=${page.page - 1}`} rel="prev">
            Precedente
          </Link>
        ) : (
          <span />
        )}
        {page.hasMore && (
          <Link to={`?page=${page.page + 1}`} rel="next">
            Successiva
          </Link>
        )}
      </nav>
    </section>
  );
}

export function BlogArticle() {
  const post = useLoaderData() as BlogPostPublic;
  const metadata = post.metadata;
  const article = {
    '@context': 'https://schema.org',
    '@type': 'Article',
    headline: metadata.title,
    description: post.seoDescription || metadata.summary,
    datePublished: metadata.publishedAt,
    dateModified: post.updatedAt,
    author: { '@type': 'Person', name: metadata.author },
    ...(metadata.coverUrl ? { image: metadata.coverUrl } : {}),
  };
  return (
    <article className="blog-surface p-3 p-md-4 bg-white">
      <SEO
        title={post.seoTitle || metadata.title}
        description={post.seoDescription || metadata.summary}
        imageUrl={metadata.coverUrl ?? undefined}
        type="article"
      />
      <Helmet>
        <script type="application/ld+json">
          {JSON.stringify(article).replace(/</g, '\\u003c')}
        </script>
      </Helmet>
      <Link to="/blog">Blog</Link>
      <header className="my-4">
        <h1>{metadata.title}</h1>
        <p className="text-secondary">
          {metadata.author}
          {metadata.author && ' · '}
          <time dateTime={metadata.publishedAt}>{metadata.publishedAt.slice(0, 10)}</time>
        </p>
        {metadata.summary && <p className="lead">{metadata.summary}</p>}
        {metadata.coverUrl && (
          <img src={metadata.coverUrl} alt={metadata.coverAlt} className="img-fluid w-100" />
        )}
      </header>
      <BlogRenderer document={post.document} images={post.images} />
      {!!metadata.tags.length && (
        <ul className="list-inline border-top pt-3">
          {metadata.tags.map((tag) => (
            <li key={tag} className="list-inline-item text-secondary">
              {tag}
            </li>
          ))}
        </ul>
      )}
    </article>
  );
}
