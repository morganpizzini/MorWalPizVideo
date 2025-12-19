/**
 * Represents a query link in the application
 * @property categoryId - Unique identifier for the query link
 * @property title - Title of the query link
 * @property description - Description of the query link
 */
export interface Category {
  /** Unique identifier for the category */
  categoryId: string;

  /** Title of the category */
  title: string;

  /** Description of the category */
  description: string;
}

/**
 * Type for creating a new category (all fields required except id which may be generated)
 */
export type CreateCategoryDTO = Omit<Category, 'categoryId'> & {
  categoryId?: string;
};

/**
 * Type for updating an existing category (all fields optional except id)
 */
export type UpdateCategoryDTO = Partial<Omit<Category, 'categoryId'>> & {
  categoryId?: string;
};
