const CART_STORAGE_KEY = 'morwalpiz_shop_cart';

export interface LocalCartItem {
  productId: string;
  productName: string;
  quantity: number;
  price?: number;
}

export interface LocalCart {
  items: LocalCartItem[];
  updatedAt: number;
}

/**
 * Get cart from localStorage
 */
export function getLocalCart(): LocalCart {
  const stored = localStorage.getItem(CART_STORAGE_KEY);
  if (!stored) {
    return { items: [], updatedAt: Date.now() };
  }

  try {
    return JSON.parse(stored) as LocalCart;
  } catch {
    return { items: [], updatedAt: Date.now() };
  }
}

/**
 * Save cart to localStorage
 */
export function saveLocalCart(cart: LocalCart): void {
  cart.updatedAt = Date.now();
  localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(cart));
}

/**
 * Add item to cart
 */
export function addToLocalCart(item: LocalCartItem): LocalCart {
  const cart = getLocalCart();
  
  // Check if item already exists
  const existingIndex = cart.items.findIndex((i) => i.productId === item.productId);
  
  if (existingIndex >= 0) {
    // Update quantity
    cart.items[existingIndex].quantity += item.quantity;
  } else {
    // Add new item
    cart.items.push(item);
  }
  
  saveLocalCart(cart);
  return cart;
}

/**
 * Remove item from cart
 */
export function removeFromLocalCart(productId: string): LocalCart {
  const cart = getLocalCart();
  cart.items = cart.items.filter((item) => item.productId !== productId);
  saveLocalCart(cart);
  return cart;
}

/**
 * Update item quantity
 */
export function updateLocalCartItemQuantity(productId: string, quantity: number): LocalCart {
  const cart = getLocalCart();
  const item = cart.items.find((i) => i.productId === productId);
  
  if (item) {
    if (quantity <= 0) {
      return removeFromLocalCart(productId);
    }
    item.quantity = quantity;
    saveLocalCart(cart);
  }
  
  return cart;
}

/**
 * Clear cart
 */
export function clearLocalCart(): void {
  localStorage.removeItem(CART_STORAGE_KEY);
}

/**
 * Get cart item count
 */
export function getCartItemCount(): number {
  const cart = getLocalCart();
  return cart.items.reduce((sum, item) => sum + item.quantity, 0);
}

/**
 * Get cart total price
 */
export function getCartTotal(): number {
  const cart = getLocalCart();
  return cart.items.reduce((sum, item) => sum + (item.price || 0) * item.quantity, 0);
}