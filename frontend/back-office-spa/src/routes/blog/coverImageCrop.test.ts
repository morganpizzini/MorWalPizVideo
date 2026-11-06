import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  COVER_IMAGE_HEIGHT,
  COVER_IMAGE_MAX_BYTES,
  COVER_IMAGE_WIDTH,
  getCoverCropBounds,
  prepareCoverImage,
} from './coverImageCrop';

afterEach(() => vi.restoreAllMocks());

describe('cover crop geometry', () => {
  it.each([
    [2400, 1600, 2400, 1350],
    [1600, 2400, 1600, 900],
    [1920, 1080, 1920, 1080],
  ])(
    'starts with the largest centered 16:9 crop for %i × %i',
    (width, height, cropWidth, cropHeight) => {
      const crop = getCoverCropBounds(width, height, 1, {
        x: width / 2,
        y: height / 2,
      });

      expect(crop).toEqual({
        x: (width - cropWidth) / 2,
        y: (height - cropHeight) / 2,
        width: cropWidth,
        height: cropHeight,
      });
      expect(crop.width / crop.height).toBeCloseTo(16 / 9);
    }
  );

  it('clamps a zoomed crop to each source edge without leaving empty space', () => {
    const crop = getCoverCropBounds(2400, 1600, 2, { x: 10_000, y: -10_000 });

    expect(crop.x + crop.width).toBeCloseTo(2400);
    expect(crop.y).toBe(0);
    expect(crop.width).toBeLessThan(2400);
    expect(crop.height).toBeLessThan(1600);
  });

  it('always represents an exact Full HD output ratio, including fractional crops', () => {
    const crop = getCoverCropBounds(4033, 3025, 1.37, { x: 1357.25, y: 1702.5 });

    expect(crop.x).toBeGreaterThanOrEqual(0);
    expect(crop.y).toBeGreaterThanOrEqual(0);
    expect(crop.x + crop.width).toBeLessThanOrEqual(4033);
    expect(crop.y + crop.height).toBeLessThanOrEqual(3025);
    expect(crop.width / crop.height).toBeCloseTo(COVER_IMAGE_WIDTH / COVER_IMAGE_HEIGHT);
  });

  it('exports the selected source rectangle as an exact Full HD JPEG', async () => {
    const drawImage = vi.fn();
    const context = {
      fillStyle: '',
      fillRect: vi.fn(),
      drawImage,
    } as unknown as CanvasRenderingContext2D;
    vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockImplementation(() => context);
    vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(function (
      this: HTMLCanvasElement,
      callback,
      type,
      quality
    ) {
      expect(this.width).toBe(COVER_IMAGE_WIDTH);
      expect(this.height).toBe(COVER_IMAGE_HEIGHT);
      expect(type).toBe('image/jpeg');
      expect(quality).toBe(0.9);
      callback(new Blob(['jpeg'], { type: 'image/jpeg' }));
    });

    const sourceFile = new File(['source'], 'editorial-cover.png', { type: 'image/png' });
    const image = {} as HTMLImageElement;
    const crop = { x: 13.5, y: 22.25, width: 1600, height: 900 };
    const prepared = await prepareCoverImage(sourceFile, image, crop);

    expect(context.fillRect).toHaveBeenCalledWith(0, 0, COVER_IMAGE_WIDTH, COVER_IMAGE_HEIGHT);
    expect(drawImage).toHaveBeenCalledWith(
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
    expect(prepared).toEqual(
      expect.objectContaining({
        name: 'editorial-cover.jpg',
        type: 'image/jpeg',
      })
    );
  });

  it('rejects a prepared JPEG that exceeds the existing upload size limit', async () => {
    const context = {
      fillStyle: '',
      fillRect: vi.fn(),
      drawImage: vi.fn(),
    } as unknown as CanvasRenderingContext2D;
    vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockImplementation(() => context);
    const oversizedJpeg = new Blob(['jpeg'], { type: 'image/jpeg' });
    Object.defineProperty(oversizedJpeg, 'size', { value: COVER_IMAGE_MAX_BYTES + 1 });
    vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(callback => {
      callback(oversizedJpeg);
    });

    await expect(
      prepareCoverImage(
        new File(['source'], 'cover.png', { type: 'image/png' }),
        {} as HTMLImageElement,
        { x: 0, y: 0, width: 16, height: 9 }
      )
    ).rejects.toThrow('prepared image exceeds 10 MB');
  });
});
