import React from 'react';
import { Button } from 'react-bootstrap';
import { Link } from 'react-router';
import GenericTable from './Table/GenericTable';
import { Match, Video } from '../models/video/types';
import { ColumnDef } from '@tanstack/react-table';

interface VideoListProps {
  matches: Match[];
}

const VideoList: React.FC<VideoListProps> = ({ matches }) => {
  // Flatten matches to individual videos for the table
  const videos = matches.flatMap((match) => 
    match.videoRefs?.map((videoRef) => ({
      id: `${match.id}-${videoRef.youtubeId}`,
      matchId: match.id,
      youtubeId: videoRef.youtubeId,
      title: match.title,
      description: match.description,
      category: videoRef.category,
      matchType: match.matchType,
      url: match.url,
      thumbnailVideoId: match.thumbnailVideoId
    })) || []
  );

  const columns: ColumnDef<any, any>[] = [
    {
      header: 'YouTube ID',
      accessorKey: 'youtubeId',
      cell: ({ getValue }) => (
        <code className="text-primary">{getValue() as string}</code>
      ),
    },
    {
      header: 'Title',
      accessorKey: 'title',
      cell: ({ getValue }) => (
        <div className="fw-semibold">{getValue() as string}</div>
      ),
    },
    {
      header: 'Category',
      accessorKey: 'category',
      cell: ({ getValue }) => (
        <span className="badge bg-secondary">{getValue() as string}</span>
      ),
    },
    {
      header: 'Match Type',
      accessorKey: 'matchType',
      cell: ({ getValue }) => (
        <span className="badge bg-info">
          {getValue() === 0 ? 'Single Video' : 'Collection'}
        </span>
      ),
    },
    {
      header: 'Actions',
      id: 'actions',
      cell: ({ row }) => (
        <div className="d-flex gap-2">
          <Button
            variant="outline-primary"
            size="sm"
            onClick={() => window.location.href = `/videos/${row.original.matchId}`}
          >
            View
          </Button>
          <Button
            variant="outline-secondary"
            size="sm"
            onClick={() => window.location.href = `/videos/${row.original.matchId}/edit`}
          >
            Edit
          </Button>
          <Button
            variant="outline-danger"
            size="sm"
            onClick={() => {
              if (window.confirm('Are you sure you want to delete this video?')) {
                // TODO: Implement delete functionality
                console.log('Delete video:', row.original.youtubeId);
              }
            }}
          >
            Delete
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="mt-5">
      <h3>Existing Videos</h3>
      <p className="text-muted mb-3">
        {videos.length} video(s) found across {matches.length} match(es)
      </p>
      <GenericTable
        data={videos}
        columns={columns}
        searchPlaceholder="Search videos by ID, title, or category..."
        emptyMessage="No videos found"
        pageSize={15}
      />
    </div>
  );
};

export default VideoList;
