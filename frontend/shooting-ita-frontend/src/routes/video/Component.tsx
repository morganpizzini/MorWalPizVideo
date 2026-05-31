import { useParams } from 'react-router-dom';
import { VideoPlayer } from '@morwalpiz/layout';

export default function VideoRoute() {
    const { youtubeId } = useParams<{ youtubeId: string }>();
    if (!youtubeId) return <div>Video not found</div>;
    return (
        <div style={{ maxWidth: 1024, margin: '0 auto' }}>
            <VideoPlayer youtubeId={youtubeId} autoplay muted />
        </div>
    );
}
