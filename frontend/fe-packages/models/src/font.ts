export interface FontCategoryResponse {
  categoryName: string;
  fontFiles: string[];
}

export interface FontListResponse {
  categories: FontCategoryResponse[];
}
