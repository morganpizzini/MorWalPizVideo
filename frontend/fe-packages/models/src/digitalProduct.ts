export interface DigitalProduct {
  id: string;
  name: string;
  description: string;
  previewImageUrl: string;
  contentStorageKey: string;
  categoryIds: string[];
  price?: number;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface ProductCategory {
  id: string;
  name: string;
  description: string;
  displayOrder?: number;
}

export interface CreateDigitalProductRequest {
  name: string;
  description: string;
  previewImageUrl: string;
  contentStorageKey: string;
  categoryIds: string[];
  price?: number;
  isActive: boolean;
}

export interface UpdateDigitalProductRequest {
  name?: string;
  description?: string;
  previewImageUrl?: string;
  contentStorageKey?: string;
  categoryIds?: string[];
  price?: number;
  isActive?: boolean;
}
