

/**
 * Interface representing a category in the system
 */
export interface Category {
  categoryId: string;
  title: string;
}

/**
 * Interface for the loader response containing categories
 */
export interface LoaderData {
  categories: Category[];
}

/**
 * Loader function that fetches categories from the API
 * @returns Promise with categories
 */
export default async function loader(): Promise<LoaderData> {
  // Fetch categories
  const categoriesPromise = fetch(`/api/categories`)
    .then(response => response.json())
    .catch(error => {
      console.error('Error loading categories:', error);
      return [];
    });

  const categories = await categoriesPromise;

  return {
    categories,
  };
}
