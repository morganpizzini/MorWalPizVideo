import { useLoaderData } from 'react-router';
import '../matches/style.scss';
import SEO from '@utils/seo';
import { trackPageView } from '../../analytics/consent';
import { CompilationVideo } from './components';
import type loader from './loader';

export type CompilationRouteData = Awaited<ReturnType<typeof loader>>;

export default function Compilations() {
  const { compilation } = useLoaderData() as CompilationRouteData;
  if (typeof window !== 'undefined') {
    trackPageView('compilation', '/compilations/:id');
  }

  return (
    <main className="compilations-page">
      <SEO
        title={compilation.title}
        description={compilation.description ?? ''}
        imageUrl={
          compilation.videos && compilation.videos.length > 0
            ? `https://img.youtube.com/vi/${compilation.videos[0].youtubeId}/hqdefault.jpg`
            : ''
        }
        type="article"
      />
      <div className="container">
        <h1 className="text-center mb-2">{compilation.title}</h1>
        {compilation.description && <p className="text-center mb-2">{compilation.description}</p>}
        <hr />

        <div id="video-container" className="row">
          {compilation.videos.map((video) => (
            <CompilationVideo key={video.youtubeId} video={video} />
          ))}
        </div>
      </div>
    </main>
  );
}
