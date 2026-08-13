// Main barrel export file for @morwalpizvideo/services

// API Service exports
export { default as apiService } from './apiService';
export {
    getSelectedChannelId,
    resetCsrfToken,
    selectFirstAccessibleChannel,
    setAuthTokenProvider,
    setCookieOnlyMode,
    setRequestCredentialsMode,
    setSelectedChannelId,
    setUnauthorizedHandler
} from './apiService';

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
export { getActiveCustomForms, getCustomFormByUrl, submitCustomFormResponse } from './apiService';

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

// Video ↔ Channel join (FR-016 / FR-017)
export { loadChannelMap, buildOwnerMap, resolveOwner, MORWALPIZ_CHANNEL_ID } from './videoChannelMap';
export type { ChannelBadge, ChannelWithVideos, VideoLike, VideoRefLike, MatchLike } from './videoChannelMap';
