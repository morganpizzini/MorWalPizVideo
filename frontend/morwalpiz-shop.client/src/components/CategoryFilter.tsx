import type { ProductCategory } from '@morwalpizvideo/models';

interface CategoryFilterProps {
  categories: ProductCategory[];
  selectedCategoryId?: string;
  onSelectCategory: (categoryId?: string) => void;
}

export default function CategoryFilter({
  categories,
  selectedCategoryId,
  onSelectCategory,
}: CategoryFilterProps) {
  return (
    <div className="card mb-4">
      <div className="card-header">
        <h5 className="mb-0">Categorie</h5>
      </div>
      <div className="list-group list-group-flush">
        <button
          className={`list-group-item list-group-item-action ${
            !selectedCategoryId ? 'active' : ''
          }`}
          onClick={() => onSelectCategory(undefined)}
        >
          Tutte le Categorie
        </button>
        {categories.map((category) => (
          <button
            key={category.id}
            className={`list-group-item list-group-item-action ${
              selectedCategoryId === category.id ? 'active' : ''
            }`}
            onClick={() => onSelectCategory(category.id)}
          >
            {category.name}
          </button>
        ))}
      </div>
    </div>
  );
}