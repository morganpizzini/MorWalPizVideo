export const COVER_IMAGE_WIDTH = 1920;
export const COVER_IMAGE_HEIGHT = 1080;
export const COVER_IMAGE_MAX_BYTES = 10_000_000;
export const COVER_IMAGE_MAX_PIXELS = 25_000_000;
export const COVER_IMAGE_MAX_ZOOM = 4;

export interface CropBounds {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface CropCenter {
  x: number;
  y: number;
}

export function getCoverCropBounds(
  sourceWidth: number,
  sourceHeight: number,
  zoom: number,
  center: CropCenter
): CropBounds {
  if (
    !Number.isFinite(sourceWidth) ||
    !Number.isFinite(sourceHeight) ||
    sourceWidth <= 0 ||
    sourceHeight <= 0
  ) {
    throw new Error('The image has invalid dimensions.');
  }

  const safeZoom = Math.min(Math.max(zoom, 1), COVER_IMAGE_MAX_ZOOM);
  const width = Math.min(sourceWidth, (sourceHeight * 16) / 9) / safeZoom;
  const height = width * (9 / 16);
  const minX = width / 2;
  const minY = height / 2;
  const maxX = sourceWidth - minX;
  const maxY = sourceHeight - minY;
  const x = Math.min(Math.max(center.x, minX), maxX);
  const y = Math.min(Math.max(center.y, minY), maxY);

  return { x: x - minX, y: y - minY, width, height };
}

export function getCropCenterFromPosition(
  sourceWidth: number,
  sourceHeight: number,
  bounds: CropBounds,
  horizontal: number,
  vertical: number
): CropCenter {
  const maxX = Math.max(0, sourceWidth - bounds.width);
  const maxY = Math.max(0, sourceHeight - bounds.height);
  return {
    x: bounds.width / 2 + (maxX * Math.min(Math.max(horizontal, 0), 100)) / 100,
    y: bounds.height / 2 + (maxY * Math.min(Math.max(vertical, 0), 100)) / 100,
  };
}

export async function prepareCoverImage(
  file: File,
  image: HTMLImageElement,
  crop: CropBounds
): Promise<File> {
  const canvas = document.createElement('canvas');
  canvas.width = COVER_IMAGE_WIDTH;
  canvas.height = COVER_IMAGE_HEIGHT;
  const context = canvas.getContext('2d');
  if (!context) throw new Error('Image preparation is unavailable in this browser.');

  context.fillStyle = '#fff';
  context.fillRect(0, 0, canvas.width, canvas.height);
  context.drawImage(
    image,
    crop.x,
    crop.y,
    crop.width,
    crop.height,
    0,
    0,
    COVER_IMAGE_WIDTH,
    COVER_IMAGE_HEIGHT
  );

  const blob = await new Promise<Blob>((resolve, reject) => {
    canvas.toBlob(
      result =>
        result
          ? resolve(result)
          : reject(new Error('The cropped image could not be prepared. Try again.')),
      'image/jpeg',
      0.9
    );
  });
  if (blob.type !== 'image/jpeg')
    throw new Error('This browser could not prepare a JPEG cover. Try another browser.');
  if (blob.size > COVER_IMAGE_MAX_BYTES)
    throw new Error('The prepared image exceeds 10 MB. Choose a simpler image and try again.');
  const stem = file.name.replace(/\.[^.]+$/, '') || 'cover';
  return new File([blob], `${stem}.jpg`, { type: 'image/jpeg' });
}
