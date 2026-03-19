export interface Category {
    categoryId: string;
    title: string;
    description: string;
}
export type CreateCategoryDTO = Omit<Category, 'categoryId'> & {
    categoryId?: string;
};
export type UpdateCategoryDTO = Partial<Omit<Category, 'categoryId'>> & {
    categoryId?: string;
};
//# sourceMappingURL=categories.d.ts.map