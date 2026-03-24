/**
 * API Service Facade
 * This file re-exports from @morwalpizvideo/services to maintain backward compatibility
 * with existing imports while centralizing the implementation in the shared package.
 */

// Re-export all HTTP methods and utilities from shared services
export {
    get,
    post,
    put,
    patch,
    Delete,
    postFormData,
    getFile,
    call,
    setAuthTokenProvider
} from '@morwalpizvideo/services';

// Re-export entity-specific service functions
export {
    fetchProducts,
    getProduct,
    createProduct,
    updateProduct,
    deleteProduct,
    fetchProductCategories,
    getProductCategory,
    createProductCategory,
    updateProductCategory,
    deleteProductCategory,
    fetchSponsors,
    getSponsor,
    createSponsor,
    createSponsorWithImage,
    updateSponsor,
    updateSponsorWithImage,
    deleteSponsor
} from '@morwalpizvideo/services';

// Re-export default apiService object
export { default } from '@morwalpizvideo/services';