import { VideoPlayer } from '@morwalpiz/layout';
import type { VideoRef } from '@morwalpizvideo/models';

interface CompilationVideoProps {
  video: VideoRef;
}

export default function CompilationVideo({ video }: CompilationVideoProps) {
  return (
    <div className="col-6 mb-3">
      <div className="video-block border rounded">
        <div className="video-thumbnail">
          <VideoPlayer youtubeId={video.youtubeId} height="180px" />
        </div>
        <div className="video-details">
          <div className="video-title">{video.title}</div>
        </div>
      </div>
    </div>
  );
}
