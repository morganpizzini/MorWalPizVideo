import { CategoryRef } from './video/types';

export interface Product {
  id: string;
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
