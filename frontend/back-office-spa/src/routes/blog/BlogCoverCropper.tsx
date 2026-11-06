import { useEffect, useRef, useState } from 'react';
import type { CSSProperties, PointerEvent } from 'react';
import { RotateCcw, Scissors } from 'lucide-react';
import {
  COVER_IMAGE_HEIGHT,
  COVER_IMAGE_MAX_BYTES,
  COVER_IMAGE_MAX_PIXELS,
  COVER_IMAGE_MAX_ZOOM,
  COVER_IMAGE_WIDTH,
  getCoverCropBounds,
  getCropCenterFromPosition,
  prepareCoverImage,
} from './coverImageCrop';
import './BlogCoverCropper.css';

interface BlogCoverCropperProps {
  file: File;
  preparedFile: File | null;
  disabled: boolean;
  onPrepared: (file: File | null) => void;
  onCancel: () => void;
}

interface LoadedImage {
  image: HTMLImageElement;
  width: number;
  height: number;
}

interface DragStart {
  pointerX: number;
  pointerY: number;
  centerX: number;
  centerY: number;
  cropWidth: number;
  cropHeight: number;
  stageWidth: number;
  stageHeight: number;
}

const acceptedTypes = new Set(['image/jpeg', 'image/png', 'image/webp']);

export function BlogCoverCropper({
  file,
  preparedFile,
  disabled,
  onPrepared,
  onCancel,
}: BlogCoverCropperProps) {
  const [loaded, setLoaded] = useState<LoadedImage | null>(null);
  const [loadError, setLoadError] = useState('');
  const [exportError, setExportError] = useState('');
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);
  const [zoom, setZoom] = useState(1);
  const [center, setCenter] = useState({ x: 0, y: 0 });
  const [preparedUrl, setPreparedUrl] = useState('');
  const dragStart = useRef<DragStart | null>(null);
  const exportRun = useRef(0);

  useEffect(() => {
    setLoaded(null);
    setLoadError('');
    setExportError('');
    setLoading(true);
    setZoom(1);
    onPrepared(null);

    if (!acceptedTypes.has(file.type)) {
      setLoadError('Choose a JPEG, PNG, or WebP image.');
      setLoading(false);
      return;
    }
    if (file.size > COVER_IMAGE_MAX_BYTES) {
      setLoadError('Choose an image smaller than 10 MB.');
      setLoading(false);
      return;
    }

    const objectUrl = URL.createObjectURL(file);
    const image = new Image();
    let active = true;
    image.onload = () => {
      if (!active) return;
      const { naturalWidth: width, naturalHeight: height } = image;
      if (!width || !height || width * height > COVER_IMAGE_MAX_PIXELS) {
        setLoadError('Choose an image with valid dimensions and no more than 25 megapixels.');
      } else {
        setLoaded({ image, width, height });
        setCenter({ x: width / 2, y: height / 2 });
      }
      setLoading(false);
    };
    image.onerror = () => {
      if (!active) return;
      setLoadError('This image could not be opened. Choose another JPEG, PNG, or WebP image.');
      setLoading(false);
    };
    image.src = objectUrl;

    return () => {
      active = false;
      exportRun.current += 1;
      image.onload = null;
      image.onerror = null;
      URL.revokeObjectURL(objectUrl);
    };
  }, [file, onPrepared]);

  useEffect(() => {
    if (!preparedFile) {
      setPreparedUrl('');
      return;
    }
    const objectUrl = URL.createObjectURL(preparedFile);
    setPreparedUrl(objectUrl);
    return () => URL.revokeObjectURL(objectUrl);
  }, [preparedFile]);

  const crop = loaded ? getCoverCropBounds(loaded.width, loaded.height, zoom, center) : null;
  const imageStyle: CSSProperties | undefined =
    loaded && crop
      ? {
          width: `${(loaded.width / crop.width) * 100}%`,
          height: `${(loaded.height / crop.height) * 100}%`,
          left: `${(-crop.x / crop.width) * 100}%`,
          top: `${(-crop.y / crop.height) * 100}%`,
        }
      : undefined;
  const horizontal =
    loaded && crop && loaded.width > crop.width
      ? ((center.x - crop.width / 2) / (loaded.width - crop.width)) * 100
      : 50;
  const vertical =
    loaded && crop && loaded.height > crop.height
      ? ((center.y - crop.height / 2) / (loaded.height - crop.height)) * 100
      : 50;

  const updateCenterFromPosition = (nextHorizontal: number, nextVertical: number) => {
    if (!loaded || !crop) return;
    setCenter(
      getCropCenterFromPosition(loaded.width, loaded.height, crop, nextHorizontal, nextVertical)
    );
  };

  const startDrag = (event: PointerEvent<HTMLDivElement>) => {
    if (!loaded || !crop || disabled || exporting || preparedFile) return;
    const rect = event.currentTarget.getBoundingClientRect();
    dragStart.current = {
      pointerX: event.clientX,
      pointerY: event.clientY,
      centerX: center.x,
      centerY: center.y,
      cropWidth: crop.width,
      cropHeight: crop.height,
      stageWidth: rect.width,
      stageHeight: rect.height,
    };
    event.currentTarget.setPointerCapture(event.pointerId);
  };

  const moveDrag = (event: PointerEvent<HTMLDivElement>) => {
    const origin = dragStart.current;
    if (!origin || !loaded) return;
    const dx = origin.stageWidth
      ? ((event.clientX - origin.pointerX) * origin.cropWidth) / origin.stageWidth
      : 0;
    const dy = origin.stageHeight
      ? ((event.clientY - origin.pointerY) * origin.cropHeight) / origin.stageHeight
      : 0;
    const maxX = Math.max(0, loaded.width - origin.cropWidth);
    const maxY = Math.max(0, loaded.height - origin.cropHeight);
    setCenter(
      getCropCenterFromPosition(
        loaded.width,
        loaded.height,
        { x: 0, y: 0, width: origin.cropWidth, height: origin.cropHeight },
        maxX ? ((origin.centerX - dx - origin.cropWidth / 2) / maxX) * 100 : 50,
        maxY ? ((origin.centerY - dy - origin.cropHeight / 2) / maxY) * 100 : 50
      )
    );
  };

  const prepare = async () => {
    if (!loaded || !crop || disabled || exporting || preparedFile) return;
    const run = ++exportRun.current;
    setExporting(true);
    setExportError('');
    try {
      const output = await prepareCoverImage(file, loaded.image, crop);
      if (exportRun.current === run) onPrepared(output);
    } catch (error) {
      if (exportRun.current === run)
        setExportError(
          error instanceof Error ? error.message : 'Unable to prepare the cover image.'
        );
    } finally {
      if (exportRun.current === run) setExporting(false);
    }
  };

  const reset = () => {
    if (!loaded) return;
    setZoom(1);
    setCenter({ x: loaded.width / 2, y: loaded.height / 2 });
    setExportError('');
  };

  return (
    <div className="blog-cover-crop" aria-busy={loading || exporting}>
      {loading && <p role="status">Opening image…</p>}
      {loadError && (
        <div className="mb-3">
          <p className="alert alert-danger py-2" role="alert">
            {loadError}
          </p>
          <button
            type="button"
            className="btn btn-sm btn-outline-secondary"
            onClick={onCancel}
            disabled={disabled}
          >
            Cancel image
          </button>
        </div>
      )}
      {preparedFile && preparedUrl ? (
        <div className="mb-3">
          <figure className="mb-2">
            <img
              className="blog-cover-crop__prepared"
              src={preparedUrl}
              alt="Prepared cover preview, 1920 by 1080 pixels"
            />
            <figcaption className="small text-secondary mt-1">
              Prepared JPEG · {COVER_IMAGE_WIDTH} × {COVER_IMAGE_HEIGHT} pixels
            </figcaption>
          </figure>
          <div className="d-flex flex-wrap gap-2">
            <button
              type="button"
              className="btn btn-sm btn-outline-secondary"
              onClick={() => onPrepared(null)}
              disabled={disabled || exporting}
            >
              Adjust crop
            </button>
            <button
              type="button"
              className="btn btn-sm btn-outline-secondary"
              onClick={onCancel}
              disabled={disabled || exporting}
            >
              Cancel image
            </button>
          </div>
        </div>
      ) : (
        loaded &&
        crop && (
          <>
            <p className="small text-secondary">
              Drag the image or adjust its position and zoom. The frame is 16:9.
            </p>
            <div
              className="blog-cover-crop__stage"
              role="group"
              aria-label="16 by 9 cover crop preview"
              onPointerDown={startDrag}
              onPointerMove={moveDrag}
              onPointerUp={() => {
                dragStart.current = null;
              }}
              onPointerCancel={() => {
                dragStart.current = null;
              }}
            >
              <img src={loaded.image.src} alt="" draggable={false} style={imageStyle} />
              <div className="blog-cover-crop__grid" aria-hidden="true">
                <span />
                <span />
                <span />
                <span />
              </div>
            </div>
            {crop.width < COVER_IMAGE_WIDTH || crop.height < COVER_IMAGE_HEIGHT ? (
              <p className="small text-warning-emphasis mt-2 mb-0" role="status">
                The selected area is smaller than Full HD and will be enlarged.
              </p>
            ) : null}
            <div className="row g-3 mt-1">
              <label className="col-md-4">
                Horizontal position
                <input
                  type="range"
                  className="form-range"
                  aria-label="Horizontal crop position"
                  min="0"
                  max="100"
                  value={horizontal}
                  disabled={disabled || exporting}
                  onChange={event => updateCenterFromPosition(Number(event.target.value), vertical)}
                />
              </label>
              <label className="col-md-4">
                Vertical position
                <input
                  type="range"
                  className="form-range"
                  aria-label="Vertical crop position"
                  min="0"
                  max="100"
                  value={vertical}
                  disabled={disabled || exporting}
                  onChange={event =>
                    updateCenterFromPosition(horizontal, Number(event.target.value))
                  }
                />
              </label>
              <label className="col-md-4">
                Zoom
                <input
                  type="range"
                  className="form-range"
                  aria-label="Crop zoom"
                  min="1"
                  max={COVER_IMAGE_MAX_ZOOM}
                  step="0.01"
                  value={zoom}
                  disabled={disabled || exporting}
                  onChange={event => {
                    const nextZoom = Number(event.target.value);
                    setZoom(nextZoom);
                    const nextCrop = getCoverCropBounds(
                      loaded.width,
                      loaded.height,
                      nextZoom,
                      center
                    );
                    setCenter({
                      x: nextCrop.x + nextCrop.width / 2,
                      y: nextCrop.y + nextCrop.height / 2,
                    });
                  }}
                />
              </label>
            </div>
            <div className="d-flex flex-wrap gap-2 mt-2">
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary"
                onClick={reset}
                disabled={disabled || exporting}
              >
                <RotateCcw size={15} className="me-1" />
                Reset crop
              </button>
              <button
                type="button"
                className="btn btn-sm btn-primary"
                onClick={prepare}
                disabled={disabled || exporting}
              >
                <Scissors size={15} className="me-1" />
                {exporting ? 'Preparing…' : 'Use this crop'}
              </button>
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary"
                onClick={onCancel}
                disabled={disabled || exporting}
              >
                Cancel image
              </button>
            </div>
            {exportError && (
              <p className="alert alert-danger py-2 mt-2 mb-0" role="alert">
                {exportError}
              </p>
            )}
          </>
        )
      )}
    </div>
  );
}
