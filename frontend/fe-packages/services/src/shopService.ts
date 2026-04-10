import { get, post, put, Delete } from './apiService';
import endpoints, { ComposeUrl } from './endpoints';
import type {
  DigitalProduct,
  CreateDigitalProductRequest,
  UpdateDigitalProductRequest,
  VideoProductCategory,
  EmailLoginRequest,
  EmailVerificationRequest,
  LoginResponse,
  Cart,
  AddToCartRequest,
  UpdateCartItemRequest,
  CheckoutRequest,
  CheckoutResponse,
  LegalContent,
  LegalContentType,
  CreateLegalContentRequest,
  UpdateLegalContentRequest,
} from '@morwalpizvideo/models';

// ============================================
// Digital Products
// ============================================

export async function fetchShopProducts(): Promise<DigitalProduct[]> {
  return await get(endpoints.SHOP_PRODUCTS);
}

export async function getShopProduct(productId: string): Promise<DigitalProduct> {
  const url = ComposeUrl(endpoints.SHOP_PRODUCTS_DETAIL, { productId });
  return await get(url);
}

export async function createShopProduct(
  data: CreateDigitalProductRequest
): Promise<DigitalProduct> {
  return await post(endpoints.SHOP_PRODUCTS, data);
}

export async function updateShopProduct(
  productId: string,
  data: UpdateDigitalProductRequest
): Promise<DigitalProduct> {
  const url = ComposeUrl(endpoints.SHOP_PRODUCTS_DETAIL, { productId });
  return await put(url, data);
}

export async function deleteShopProduct(productId: string): Promise<void> {
  const url = ComposeUrl(endpoints.SHOP_PRODUCTS_DETAIL, { productId });
  return await Delete(url);
}

// ============================================
// Product Categories
// ============================================

export async function fetchShopProductCategories(): Promise<VideoProductCategory[]> {
  return await get(endpoints.SHOP_PRODUCT_CATEGORIES);
}

// ============================================
// Authentication
// ============================================

export async function shopLogin(data: EmailLoginRequest): Promise<LoginResponse> {
  return await post(endpoints.SHOP_AUTH_LOGIN, data);
}

export async function shopVerifyEmail(
  data: EmailVerificationRequest
): Promise<LoginResponse> {
  return await post(endpoints.SHOP_AUTH_VERIFY, data);
}

// ============================================
// Cart
// ============================================

export async function getShopCart(): Promise<Cart> {
  return await get(endpoints.SHOP_CART);
}

export async function addToCart(data: AddToCartRequest): Promise<Cart> {
  return await post(endpoints.SHOP_CART_ITEMS, data);
}

export async function updateCartItem(data: UpdateCartItemRequest): Promise<Cart> {
  return await put(endpoints.SHOP_CART_ITEMS, data);
}

export async function removeFromCart(productId: string): Promise<Cart> {
  const url = `${endpoints.SHOP_CART_ITEMS}/${productId}`;
  return await Delete(url);
}

export async function checkoutCart(data: CheckoutRequest): Promise<CheckoutResponse> {
  return await post(endpoints.SHOP_CART_CHECKOUT, data);
}

// ============================================
// Legal Content
// ============================================

export async function getLegalContent(type: LegalContentType): Promise<LegalContent> {
  const url = ComposeUrl(endpoints.SHOP_LEGAL, { type });
  return await get(url);
}

export async function createLegalContent(
  data: CreateLegalContentRequest
): Promise<LegalContent> {
  const url = ComposeUrl(endpoints.SHOP_LEGAL, { type: data.type });
  return await post(url, data);
}

export async function updateLegalContent(
  type: LegalContentType,
  data: UpdateLegalContentRequest
): Promise<LegalContent> {
  const url = ComposeUrl(endpoints.SHOP_LEGAL, { type });
  return await put(url, data);
}