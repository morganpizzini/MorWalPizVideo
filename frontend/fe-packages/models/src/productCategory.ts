export interface ProductCategory {
  id: string;
  channelId?: string;
  title: string;
  description: string;
  creationDateTime: string;
}

export interface CreateProductCategoryDTO {
  title: string;
  description: string;
}

export interface UpdateProductCategoryDTO {
  title: string;
  description: string;
}
