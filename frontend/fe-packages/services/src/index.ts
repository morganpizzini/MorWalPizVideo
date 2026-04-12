// Main barrel export file for @morwalpizvideo/services

// API Service exports
export { default as apiService } from './apiService';
export { setAuthTokenProvider, setRequestCredentialsMode } from './apiService';

// Export individual HTTP methods
export { get, post, put, patch, Delete, postFormData, getFile, call } from './apiService';

// Export entity-specific service functions
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
} from './apiService';

// Endpoints exports
export { default as endpoints } from './endpoints';
export { default as frontendEndpoints } from './endpoints-frontend';
export { ComposeUrl } from './endpoints';

// Shop service exports
export {
    fetchShopProducts,
    getShopProduct,
    createShopProduct,
    updateShopProduct,
    deleteShopProduct,
    fetchShopProductCategories,
    shopLogin,
    shopVerifyEmail,
    getShopCart,
    addToCart,
    updateCartItem,
    removeFromCart,
    checkoutCart,
    getLegalContent,
    createLegalContent,
    updateLegalContent
} from './shopService';

// Insights service exports
export {
    insightsTopicsApi,
    insightsNewsApi,
    insightsContentPlansApi
} from './insightsService';
