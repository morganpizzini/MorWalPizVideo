export interface Cart {
  id: string;
  customerId: string;
  items: CartItem[];
  isCompleted: boolean;
  createdAt: Date;
  updatedAt: Date;
  completedAt?: Date;
}

export interface CartItem {
  productId: string;
  productName: string;
  quantity: number;
  price?: number;
}

export interface AddToCartRequest {
  productId: string;
  quantity: number;
}

export interface UpdateCartItemRequest {
  productId: string;
  quantity: number;
}

export interface CheckoutRequest {
  idempotencyKey: string;
}

export interface CheckoutResponse {
  orderId: string;
  status:
    | "PendingPayment"
    | "PaymentConfirmed"
    | "Cancelled"
    | "Fulfilled"
    | "AccessRevoked";
  paymentStatus: "Pending" | "Confirmed";
  totalAmount: number;
}
