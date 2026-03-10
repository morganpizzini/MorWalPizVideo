import React from 'react';
import { Card, Badge, Button } from 'react-bootstrap';
import { Link, useLoaderData } from 'react-router';
import { Compilation } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';

const CompilationDetail: React.FC = () => {
  const compilation = useLoaderData<Compilation>();

  return (
    <>
      <PageHeader title="Compilation Details" />
      
      <Card className="mb-3">
        <Card.Header as="h5">Basic Information</Card.Header>
        <Card.Body>
          <div className="mb-3">
            <strong>ID:</strong>
            <p className="mb-0">{compilation.id}</p>
          </div>
          <div className="mb-3">
            <strong>Title:</strong>
            <p className="mb-0">{compilation.title}</p>
          </div>
          <div className="mb-3">
            <strong>Description:</strong>
            <p className="mb-0">{compilation.description}</p>
          </div>
          <div className="mb-3">
            <strong>URL:</strong>
            <p className="mb-0">
              <a href={compilation.url} target="_blank" rel="noopener noreferrer">
                {compilation.url}
              </a>
            </p>
          </div>
        </Card.Body>
      </Card>

      <Card className="mb-3">
        <Card.Header as="h5">
          Videos ({compilation.videos?.length || 0})
        </Card.Header>
        <Card.Body>
          {compilation.videos && compilation.videos.length > 0 ? (
            <div className="d-flex flex-column gap-3">
              {compilation.videos.map((video, index) => (
                <Card key={video.youtubeId} className="border">
                  <Card.Body>
                    <div className="d-flex justify-content-between align-items-start">
                      <div>
                        <h6 className="mb-1">
                          {index + 1}. {video.title || video.youtubeId}
                        </h6>
                        <p className="text-muted mb-2">
                          <small>YouTube ID: {video.youtubeId}</small>
                        </p>
                        {video.description && (
                          <p className="mb-2">{video.description}</p>
                        )}
                        {video.categories && video.categories.length > 0 && (
                          <div className="d-flex gap-1 flex-wrap">
                            {video.categories.map((cat: any, idx: number) => (
                              <Badge key={idx} bg="secondary">
                                {cat.name || cat}
                              </Badge>
                            ))}
                          </div>
                        )}
                      </div>
                      <a
                        href={`https://www.youtube.com/watch?v=${video.youtubeId}`}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="btn btn-sm btn-outline-primary"
                      >
                        View on YouTube
                      </a>
                    </div>
                  </Card.Body>
                </Card>
              ))}
            </div>
          ) : (
            <p className="text-muted mb-0">No videos in this compilation</p>
          )}
        </Card.Body>
      </Card>

      <div className="d-flex gap-2">
        <Link to={`/compilations/${compilation.id}/edit`} className="btn btn-primary">
          Edit
        </Link>
        <Link to="/compilations" className="btn btn-secondary">
          Back to List
        </Link>
      </div>
    </>
  );
};

export default CompilationDetail;
