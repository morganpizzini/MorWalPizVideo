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
export function get(url: string, query?: any, overrideHeaderEnv: string = '') {
    return call(url, 'GET', {}, overrideHeaderEnv, query);
}
export function getFile(url: string, query?: any, overrideHeaderEnv: string = '') {
    return call(url, 'GET', {}, overrideHeaderEnv, query, true);
}
export function Delete(url: string, query?: any, overrideHeaderEnv: string = '') {
    return call(url, 'DELETE', {}, overrideHeaderEnv, query);
}

import { authService } from './authService';
import endpoints, { ComposeUrl } from './endpoints';
import type { Product, CreateProductDTO, UpdateProductDTO } from '@models/product';
import type { ProductCategory, CreateProductCategoryDTO, UpdateProductCategoryDTO } from '@models/productCategory';
import type { Sponsor, CreateSponsorDTO, UpdateSponsorDTO } from '@models/sponsor';

/**
 * Makes API calls with automatic authentication header injection
 */
export async function call(url: string, method: string, body: any, overrideHeaderEnv: string = '', query?: any, downloadFile = false, isFormData = false) {
    const headers = new Headers();

    // Don't set Content-Type for FormData - let the browser set it with the boundary
    if (!isFormData) {
        headers.append("Content-Type", 'application/json');
    }

    // Add authorization header if user is authenticated
    const token = authService.getToken();
    if (token) {
        headers.append("Authorization", `Bearer ${token}`);
    }

    if (query) {
        url += `?${new URLSearchParams(query).toString()}`;
    }

    const options: RequestInit = {
        method: method,
        headers: headers //,
        //credentials: "same-origin" as RequestCredentials
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
    return fetch(`/${url}`, options)
        .then(async (response) => {
            if (response.ok) {
                if (downloadFile) {
                    return response.blob();
                }
                if (response.status === 204) {
                    return Promise.resolve({});
                }
                const parsedResponse = await response.json();
                return Object.prototype.hasOwnProperty.call(parsedResponse, 'data')
                    ? parsedResponse.data
                    : parsedResponse;
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

export const fetchProductCategories = (): Promise<ProductCategory[]> =>
    get(endpoints.PRODUCTCATEGORIES);

export const getProductCategory = (id: string): Promise<ProductCategory> =>
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
