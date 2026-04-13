import endpoints, { ComposeUrl } from './endpoints';
import type { Product, CreateProductDTO, UpdateProductDTO } from '@morwalpizvideo/models';
import type { VideoProductCategory, CreateProductCategoryDTO, UpdateProductCategoryDTO } from '@morwalpizvideo/models';
import type { Sponsor, CreateSponsorDTO, UpdateSponsorDTO } from '@morwalpizvideo/models';

/**
 * Auth token provider function type
 * Applications can register their own token provider
 */
type AuthTokenProvider = () => string | null;

/**
 * Registered auth token provider
 */
let authTokenProvider: AuthTokenProvider | null = null;

/**
 * Request credentials mode type
 * Controls whether credentials (cookies, authorization headers) are included in cross-origin requests
 */
type CredentialsMode = RequestCredentials;

/**
 * Global credentials mode setting
 * Defaults to 'include' for backward compatibility with authenticated apps
 */
let requestCredentialsMode: CredentialsMode = 'include';

/**
 * Register an authentication token provider
 * This allows applications to inject their own auth logic
 * @param provider Function that returns the current auth token or null
 */
export function setAuthTokenProvider(provider: AuthTokenProvider): void {
    authTokenProvider = provider;
}

/**
 * Set the request credentials mode for all API calls
 * This controls whether credentials (cookies, authorization headers) are included in cross-origin requests
 * @param mode The credentials mode: 'include' | 'omit' | 'same-origin'
 *             - 'include': Always send credentials (default for authenticated apps)
 *             - 'omit': Never send credentials (recommended for public/unauthenticated apps)
 *             - 'same-origin': Only send credentials for same-origin requests
 */
export function setRequestCredentialsMode(mode: CredentialsMode): void {
    requestCredentialsMode = mode;
}

/**
 * Get authentication token from registered provider or localStorage fallback
 * @returns Auth token or null
 */
function getAuthToken(): string | null {
    // Try registered provider first
    if (authTokenProvider) {
        return authTokenProvider();
    }
    
    // Fallback to localStorage for backward compatibility
    if (typeof window !== 'undefined' && window.localStorage) {
        return localStorage.getItem('authToken');
    }
    
    return null;
}

/**
 * Get the API base URL from runtime environment or build-time environment
 * Priority: window.ENV (Docker runtime) > import.meta.env (Vite build-time) > relative paths
 */
function getApiBaseUrl(): string {
    // Check runtime environment (injected by Docker entrypoint)
    if (typeof window !==

 'undefined' && (window as any).ENV?.VITE_API_BASE_URL) {
        return (window as any).ENV.VITE_API_BASE_URL;
    }
    if (typeof window !== 'undefined' && (window as any).ENV?.API_BASE_URL) {
        return (window as any).ENV.API_BASE_URL;
    }
    
    // Check build-time environment (Vite)
    if (import.meta.env.VITE_API_BASE_URL) {
        return import.meta.env.VITE_API_BASE_URL;
    }
    
    // Default to relative paths (for Vite dev proxy)
    return '';
}

/**
 * Normalize URL path and optionally prepend base URL
 * Handles both '/api/...' and 'api/...' formats safely
 */
function buildFullUrl(path: string): string {
    const baseUrl = getApiBaseUrl();
    
    // Normalize path to have single leading slash
    const normalizedPath = path.startsWith('/') ? path : `/${path}`;
    
    // If no base URL, return relative path (for Vite proxy)
    if (!baseUrl) {
        return normalizedPath;
    }
    
    // Remove trailing slash from base URL
    const cleanBase = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
    
    // Combine base URL with path
    return `${cleanBase}${normalizedPath}`;
}

export function post(url: string, obj: any, overrideHeaderEnv: string = '') {
    return call(url, 'POST', obj, overrideHeaderEnv);
}

export function postFormData(url: string, formData: FormData, overrideHeaderEnv: string = '') {
    return call(url, 'POST', formData, overrideHeaderEnv, undefined, false, true);
}

export function put(url: string, obj: any, overrideHeaderEnv: string = '') {
    return call(url, 'PUT', obj, overrideHeaderEnv);
}

export function patch(url: string, obj: any, overrideHeaderEnv: string = '') {
    return call(url, 'PATCH', obj, overrideHeaderEnv);
}

export function get(url: string, query?: any, overrideHeaderEnv: string = '', returnFullRresponse: boolean = false) {
    return call(url, 'GET', {}, overrideHeaderEnv, query, false, false, { returnFullResponse: returnFullRresponse });
}

export function getFile(url: string, query?: any, overrideHeaderEnv: string = '') {
    return call(url, 'GET', {}, overrideHeaderEnv, query, true);
}

export function Delete(url: string, query?: any, overrideHeaderEnv: string = '') {
    return call(url, 'DELETE', {}, overrideHeaderEnv, query);
}

/**
 * Response options for the call method
 */
interface ResponseOptions {
    /** If true, returns the full parsed response instead of extracting the 'data' property */
    returnFullResponse?: boolean;
}

/**
 * Makes API calls with automatic authentication header injection
 * 
 * @param url - The API endpoint URL
 * @param method - HTTP method (GET, POST, PUT, etc.)
 * @param body - Request body
 * @param overrideHeaderEnv - Optional header environment override
 * @param query - Optional query parameters
 * @param downloadFile - If true, returns response as blob
 * @param isFormData - If true, treats body as FormData
 * @param responseOptions - Options for response handling
 * @returns Parsed response data. By default returns response.data if present, otherwise full response.
 *          Set responseOptions.returnFullResponse = true to always get the full response object.
 * 
 * @example
 * // Default behavior - returns response.data if present
 * const data = await call('/api/products', 'GET', {});
 * 
 * @example
 * // Get full response envelope including metadata
 * const fullResponse = await call('/api/products', 'GET', {}, '', undefined, false, false, { returnFullResponse: true });
 */
export async function call(url: string, method: string, body: any, overrideHeaderEnv: string = '', query?: any, downloadFile = false, isFormData = false, responseOptions?: ResponseOptions) {
    const headers = new Headers();

    // Don't set Content-Type for FormData - let the browser set it with the boundary
    if (!isFormData) {
        headers.append("Content-Type", 'application/json');
    }

    // Add authorization header if user is authenticated
    const token = getAuthToken();
    if (token) {
        headers.append("Authorization", `Bearer ${token}`);
    }

    if (query) {
        url += `?${new URLSearchParams(query).toString()}`;
    }

    const options: RequestInit = {
        method: method,
        headers: headers
    };

    if (body) {
        if (isFormData) {
            // For FormData, use it directly
            options.body = body as FormData;
        } else if (Object.keys(body).length > 0) {
            // For regular objects, stringify them
            options.body = JSON.stringify(body);
        }
    }
    
    // Include credentials based on configured mode
    options.credentials = requestCredentialsMode;
    
    return fetch(buildFullUrl(url), options)
        .then(async (response) => {
            if (response.ok) {
                if (downloadFile) {
                    return response.blob();
                }
                if (response.status === 204) {
                    return Promise.resolve({});
                }
                const parsedResponse = await response.json();
                
                // If returnFullResponse is true, return the entire response object
                if (responseOptions?.returnFullResponse || !Object.prototype.hasOwnProperty.call(parsedResponse, 'data'))
                    return parsedResponse;
                
                // Default behavior: return data property if it exists, otherwise return full response
                return parsedResponse.data;
            } else {
                // error handling
                const errorMessages = [];
                switch (response.status) {
                    case 400:
                        {
                            const parsedResponse = await response.json();
                            if (typeof parsedResponse === 'object') {
                                for (const key in parsedResponse) {
                                    if (Array.isArray(parsedResponse[key])) {
                                        errorMessages.push(...parsedResponse[key]);
                                    } else {
                                        errorMessages.push(parsedResponse[key]);
                                    }
                                }
                            } else {
                                errorMessages.push(parsedResponse);
                            }
                            break;
                        }
                    case 401:
                    case 429:
                        {
                            // Parse JSON response for authentication errors and rate limit errors
                            try {
                                const parsedResponse = await response.json();
                                // Return the entire parsed response to preserve structure (message, retryAfter, remainingAttempts, etc.)
                                return parsedResponse;
                            } catch {
                                errorMessages.push("Authentication failed");
                            }
                            break;
                        }
                    case 404:
                        errorMessages.push({ api: "Not found" });
                        break;
                    default:
                        errorMessages.push("An unexpected error occurred");
                }
                return {
                    errors: errorMessages
                };
            }
        })
        .catch(error => { console.log("api error", error) });
}

// ==================== Product API Services ====================

export const fetchProducts = (): Promise<Product[]> =>
    get(endpoints.PRODUCTS);

export const getProduct = (id: string): Promise<Product> =>
    get(ComposeUrl(endpoints.PRODUCTS_DETAIL, { productId: id }));

export const createProduct = (data: CreateProductDTO) =>
    post(endpoints.PRODUCTS, data);

export const updateProduct = (id: string, data: UpdateProductDTO) =>
    put(ComposeUrl(endpoints.PRODUCTS_DETAIL, { productId: id }), data);

export const deleteProduct = (id: string) =>
    Delete(ComposeUrl(endpoints.PRODUCTS_DETAIL, { productId: id }));

// ==================== ProductCategory API Services ====================

export const fetchProductCategories = (): Promise<VideoProductCategory[]> =>
    get(endpoints.PRODUCTCATEGORIES);

export const getProductCategory = (id: string): Promise<VideoProductCategory> =>
    get(ComposeUrl(endpoints.PRODUCTCATEGORIES_DETAIL, { productCategoryId: id }));

export const createProductCategory = (data: CreateProductCategoryDTO) =>
    post(endpoints.PRODUCTCATEGORIES, data);

export const updateProductCategory = (id: string, data: UpdateProductCategoryDTO) =>
    put(ComposeUrl(endpoints.PRODUCTCATEGORIES_DETAIL, { productCategoryId: id }), data);

export const deleteProductCategory = (id: string) =>
    Delete(ComposeUrl(endpoints.PRODUCTCATEGORIES_DETAIL, { productCategoryId: id }));

// ==================== Sponsor API Services ====================

export const fetchSponsors = (): Promise<Sponsor[]> =>
    get(endpoints.SPONSORS);

export const getSponsor = (id: string): Promise<Sponsor> =>
    get(ComposeUrl(endpoints.SPONSORS_DETAIL, { sponsorId: id }));

export const createSponsor = (data: CreateSponsorDTO) =>
    post(endpoints.SPONSORS, data);

export const createSponsorWithImage = (formData: FormData) =>
    postFormData(endpoints.SPONSORS, formData);

export const updateSponsor = (id: string, data: UpdateSponsorDTO) =>
    put(ComposeUrl(endpoints.SPONSORS_DETAIL, { sponsorId: id }), data);

export const updateSponsorWithImage = (id: string, formData: FormData) =>
    call(ComposeUrl(endpoints.SPONSORS_DETAIL, { sponsorId: id }), 'PUT', formData, '', undefined, false, true);

export const deleteSponsor = (id: string) =>
    Delete(ComposeUrl(endpoints.SPONSORS_DETAIL, { sponsorId: id }));

// Default export for convenience
const apiService = {
    // Core HTTP methods
    get,
    post,
    put,
    patch,
    Delete,
    postFormData,
    getFile,
    call,
    
    // Product services
    fetchProducts,
    getProduct,
    createProduct,
    updateProduct,
    deleteProduct,
    
    // ProductCategory services
    fetchProductCategories,
    getProductCategory,
    createProductCategory,
    updateProductCategory,
    deleteProductCategory,
    
    // Sponsor services
    fetchSponsors,
    getSponsor,
    createSponsor,
    createSponsorWithImage,
    updateSponsor,
    updateSponsorWithImage,
    deleteSponsor,
};

export default apiService;
