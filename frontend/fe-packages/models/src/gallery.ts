export interface GalleryImage {
  storageKey: string;
  publicUrl: string;
  previewStorageKey?: string | null;
  previewPublicUrl?: string | null;
  contentType: string;
  width: number;
  height: number;
  displayOrder: number;
}

export interface Gallery {
  id: string;
  name: string;
  description?: string | null;
  downloadCount: number;
  slug: string;
  expiration: string;
  channelId: string;
  images: GalleryImage[];
}

export interface GalleryPublic {
  name: string;
  description?: string | null;
  slug: string;
  expiration: string;
  downloadCount: number;
  images: string[];
}

export interface SaveGalleryRequest {
  name: string;
  description?: string;
  slug: string;
  expirationDays: 3 | 7 | 21;
}
