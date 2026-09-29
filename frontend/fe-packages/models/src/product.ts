import { CategoryRef } from "./video/types";

export interface Product {
  id: string;
  channelId?: string;
  title: string;
  description: string;
  url: string;
  categories: CategoryRef[];
  creationDateTime: string;
}

export interface CreateProductDTO {
  title: string;
  description: string;
  url: string;
  categoryIds: string[];
}

export interface UpdateProductDTO {
  title: string;
  description: string;
  url: string;
  categoryIds: string[];
}

export interface BulkCreateProductRowDTO {
  rowNumber: number;
  inputKey?: string;
  title: string;
  description: string;
  url: string;
  categoryIds: string[];
  categoryNames: string[];
}

export interface BulkCreateProductsDTO {
  items: BulkCreateProductRowDTO[];
}

export interface BulkCategoryAssignmentRowDTO {
  productId: string;
  inputKey?: string;
  categoryIds: string[];
}

export interface BulkCategoryAssignmentDTO {
  items: BulkCategoryAssignmentRowDTO[];
}

export interface BulkProductOperationOutcome {
  rowNumber?: number;
  inputKey?: string;
  productId?: string;
  success: boolean;
  error?: string;
}

export interface BulkProductOperationResponse {
  results: BulkProductOperationOutcome[];
}
