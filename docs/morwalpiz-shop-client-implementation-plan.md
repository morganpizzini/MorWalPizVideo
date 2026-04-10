# Piano di Implementazione: MorWalPiz Shop Client

**Data**: 25 Marzo 2026  
**Versione**: 1.0  
**Stato**: Draft

---

## 1. Executive Summary

Questo documento descrive il piano di implementazione per una nuova applicazione frontend **MorWalPiz Shop Client** (`frontend/morwalpiz-shop.client`), un portale e-commerce per la vendita di documenti digitali. L'applicazione riutilizza l'architettura e il layout della soluzione esistente `frontend/morwalpizvideo.client`, introducendo componenti di layout generalizzati e parametrici tramite un nuovo package condiviso.

### Obiettivi Principali
- Creare un nuovo shop online per documenti digitali
- Generalizzare i componenti di layout (header, footer, main) rendendoli riusabili
- Implementare autenticazione email-only con reCAPTCHA
- Gestire contenuti digitali tramite Azure Blob Storage
- Mantenere la separazione tra frontend, gateway API e business logic

---

## 2. Architettura di Riferimento

### 2.1 Monorepo Frontend Esistente

```
frontend/
├── package.json              # Yarn workspaces root
├── fe-packages/              # Shared packages
│   ├── models/              # @morwalpizvideo/models
│   ├── services/            # @morwalpizvideo/services
│   └── layout/              # @morwalpiz/layout (NUOVO)
├── morwalpizvideo.client/   # Existing public client
├── back-office-spa/         # Existing admin SPA
└── morwalpiz-shop.client/   # NEW: Digital shop SPA
```

### 2.2 Backend Architecture Split

```
┌─────────────────────────────────────────────────────────┐
│              Frontend Applications                       │
│  morwalpizvideo.client  |  morwalpiz-shop.client        │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│         MorWalPizVideo.ServerAPI (BFF/Gateway)          │
│  - Input validation                                     │
│  - reCAPTCHA verification                              │
│  - Rate limiting                                        │
│  - DTO mapping                                          │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│      MorWalPizVideo.BackOffice (Business Logic)         │
│  - Domain services                                      │
│  - Data access (MongoDB)                                │
│  - Business rules                                       │
│  - Audit & validation                                   │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│              External Services                           │
│  MongoDB | Azure Blob Storage | reCAPTCHA               │
└─────────────────────────────────────────────────────────┘
```

---

## 3. Nuovo Package: `@morwalpiz/layout`

### 3.1 Obiettivo
Estrarre e generalizzare i componenti di layout attualmente hardcoded in `morwalpizvideo.client`, rendendoli parametrici e riutilizzabili per applicazioni multiple.

### 3.2 Struttura Package

```
frontend/fe-packages/layout/morwalpiz-layout/
├── package.json
├── tsconfig.json
├── .gitignore
├── README.md
├── src/
│   ├── index.ts              # Barrel exports
│   ├── types.ts              # TypeScript interfaces
│   ├── components/
│   │   ├── AppShell.tsx
│   │   ├── SiteHeader.tsx
│   │   ├── SiteFooter.tsx
│   │   ├── PageContainer.tsx
│   │   ├── HeroHeader.tsx
│   │   ├── NavigationMenu.tsx
│   │   ├── LegalLinks.tsx
│   │   └── SocialLinks.tsx
│   ├── styles/
│   │   ├── app-shell.scss
│   │   ├── header.scss
│   │   ├── footer.scss
│   │   └── variables.scss
│   └── utils/
│       ├── layout-config.ts
│       └── theme-utility.ts
└── dist/                     # Build output
```

### 3.3 Configurazione Parametrica

#### `LayoutConfig` Interface

```typescript
export interface LayoutConfig {
  brand: BrandConfig;
  header: HeaderConfig;
  footer: FooterConfig;
  theme?: ThemeConfig;
}

export interface BrandConfig {
  name: string;
  logo?: string;
  tagline?: string;
  homeRoute?: string;
}

export interface HeaderConfig {
  navigation: NavItem[];
  socialLinks?: SocialItem[];
  showBrand?: boolean;
  hideLinks?: boolean;
  dimensions?: string; // CSS classes
}

export interface FooterConfig {
  sections: FooterSection[];
  copyright?: string;
  legalLinks?: LegalPageLink[];
}

export interface NavItem {
  label: string;
  path: string;
  external?: boolean;
  icon?: string;
}

export interface SocialItem {
  platform: 'telegram' | 'youtube' | 'instagram' | 'facebook' | 'twitter';
  url: string;
  label: string;
}

export interface LegalPageLink {
  label: string;
  path: string;
}

export interface FooterSection {
  title: string;
  links: NavItem[];
}

export interface ThemeConfig {
  primaryColor?: string;
  secondaryColor?: string;
  containerClass?: string;
}
```

### 3.4 Componenti Principali

#### `AppShell`
Layout wrapper principale con header, contenuto e footer.

```typescript
interface AppShellProps {
  config: LayoutConfig;
  children: React.ReactNode;
  loading?: boolean;
}
```

#### `SiteHeader`
Header parametrico con brand, navigazione e link social.

```typescript
interface SiteHeaderProps {
  config: HeaderConfig;
  brand: BrandConfig;
}
```

#### `SiteFooter`
Footer con sezioni configurabili e link legali.

```typescript
interface SiteFooterProps {
  config: FooterConfig;
  brand: BrandConfig;
}
```

### 3.5 package.json

```json
{
  "name": "@morwalpiz/layout",
  "version": "1.0.0",
  "description": "Reusable layout components for MorWalPiz applications",
  "main": "dist/index.js",
  "module": "dist/index.js",
  "types": "dist/index.d.ts",
  "type": "module",
  "exports": {
    ".": {
      "types": "./dist/index.d.ts",
      "import": "./dist/index.js"
    },
    "./styles": "./dist/styles/index.css"
  },
  "scripts": {
    "build": "tsc && npm run build:styles",
    "build:styles": "sass src/styles:dist/styles",
    "clean": "rm -rf dist",
    "rebuild": "npm run clean && npm run build"
  },
  "peerDependencies": {
    "react": "^19.0.0",
    "react-router": "^7.0.0"
  },
  "devDependencies": {
    "typescript": "~5.7.2",
    "sass": "^1.77.6",
    "@types/react": "^19.0.0"
  },
  "files": [
    "dist"
  ]
}
```

---

## 4. Nuova Applicazione: `morwalpiz-shop.client`

### 4.1 Struttura Target

```
frontend/morwalpiz-shop.client/
├── package.json
├── tsconfig.json
├── tsconfig.app.json
├── tsconfig.node.json
├── vite.config.ts
├── index.html
├── .env
├── .env.production
├── .dockerignore
├── .gitignore
├── .prettierrc.json
├── .prettierignore
├── eslint.config.js
├── Dockerfile
├── nginx.conf
├── docker-entrypoint.sh
├── public/
│   ├── favicon.ico
│   ├── logo.png
│   └── images/
├── src/
│   ├── main.tsx
│   ├── main.scss
│   ├── types/
│   │   └── env.d.ts
│   ├── routes/
│   │   ├── root.tsx
│   │   ├── index.tsx
│   │   ├── login.tsx
│   │   ├── catalog.tsx
│   │   ├── product-detail.tsx
│   │   ├── cart.tsx
│   │   ├── checkout-success.tsx
│   │   ├── terms-and-conditions.tsx
│   │   ├── privacy-policy.tsx
│   │   ├── cookie-policy.tsx
│   │   └── error-page.tsx
│   ├── components/
│   │   ├── ProductCard.tsx
│   │   ├── ProductGrid.tsx
│   │   ├── CartSummary.tsx
│   │   ├── CategoryFilter.tsx
│   │   └── LoginForm.tsx
│   ├── services/
│   │   ├── auth.ts
│   │   ├── catalog.ts
│   │   ├── cart.ts
│   │   └── legal.ts
│   ├── store/
│   │   ├── auth-storage.ts
│   │   └── cart-storage.ts
│   └── utils/
│       ├── layout-config.ts
│       ├── email-validator.ts
│       └── recaptcha-helper.ts
└── README.md
```

### 4.2 package.json

```json
{
  "name": "morwalpiz-shop.client",
  "version": "1.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc -b && vite build",
    "build-uncheck": "vite build",
    "preview": "vite preview",
    "lint": "eslint .",
    "format": "prettier --write \"src/**/*.{ts,tsx,css,scss}\""
  },
  "dependencies": {
    "@morwalpizvideo/models": "file:../fe-packages/models",
    "@morwalpizvideo/services": "file:../fe-packages/services",
    "@morwalpiz/layout": "file:../fe-packages/layout/morwalpiz-layout",
    "react": "19.2.4",
    "react-dom": "19.2.4",
    "react-router": "^7.4.0",
    "react-google-recaptcha-v3": "^1.10.1",
    "react-helmet-async": "^3.0.0",
    "bootstrap": "^5.3.8"
  },
  "devDependencies": {
    "@vitejs/plugin-react-swc": "^3.8.0",
    "typescript": "~5.7.2",
    "vite": "^6.2.0",
    "sass": "^1.77.6",
    "eslint": "^9.21.0",
    "prettier": "^3.5.3"
  }
}
```

### 4.3 main.tsx Bootstrap

```typescript
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createBrowserRouter, RouterProvider } from 'react-router';
import { HelmetProvider } from 'react-helmet-async';
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3';
import './main.scss';

// Routes
import Root from './routes/root';
import Index from './routes/index';
import Login from './routes/login';
import Catalog from './routes/catalog';
import ProductDetail from './routes/product-detail';
import Cart from './routes/cart';
import CheckoutSuccess from './routes/checkout-success';
import TermsAndConditions from './routes/terms-and-conditions';
import PrivacyPolicy from './routes/privacy-policy';
import CookiePolicy from './routes/cookie-policy';
import ErrorPage from './routes/error-page';

// Loaders
import catalogLoader from './routes/catalog.loader';
import productDetailLoader from './routes/product-detail.loader';

const router = createBrowserRouter([
  {
    path: '/',
    element: <Root />,
    errorElement: <ErrorPage />,
    children: [
      { index: true, element: <Index /> },
      { path: 'login', element: <Login /> },
      { 
        path: 'catalog', 
        loader: catalogLoader,
        element: <Catalog /> 
      },
      { 
        path: 'products/:productId', 
        loader: productDetailLoader,
        element: <ProductDetail /> 
      },
      { path: 'cart', element: <Cart /> },
      { path: 'checkout-success', element: <CheckoutSuccess /> },
      { path: 'terms', element: <TermsAndConditions /> },
      { path: 'privacy', element: <PrivacyPolicy /> },
      { path: 'cookie-policy', element: <CookiePolicy /> },
    ],
  },
]);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <HelmetProvider>
      <GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_RECAPTCHA_KEY}>
        <RouterProvider router={router} />
      </GoogleReCaptchaProvider>
    </HelmetProvider>
  </StrictMode>
);
```

### 4.4 routes/root.tsx

```typescript
import { Outlet } from 'react-router';
import { AppShell } from '@morwalpiz/layout';
import { layoutConfig } from '../utils/layout-config';

export default function Root() {
  return (
    <AppShell config={layoutConfig}>
      <Outlet />
    </AppShell>
  );
}
```

### 4.5 Layout Configuration

```typescript
// src/utils/layout-config.ts
import { LayoutConfig } from '@morwalpiz/layout';

export const layoutConfig: LayoutConfig = {
  brand: {
    name: 'MorWalPiz Shop',
    logo: '/images/logo.png',
    tagline: 'Documenti Digitali',
    homeRoute: '/',
  },
  header: {
    navigation: [
      { label: 'Catalogo', path: '/catalog' },
      { label: 'Carrello', path: '/cart' },
      { label: 'Login', path: '/login' },
    ],
    socialLinks: [
      { 
        platform: 'telegram', 
        url: 'https://t.me/morwalpiz', 
        label: 'Telegram' 
      },
      { 
        platform: 'instagram', 
        url: 'https://instagram.com/morwalpiz', 
        label: 'Instagram' 
      },
    ],
    showBrand: true,
  },
  footer: {
    sections: [
      {
        title: 'Pagine',
        links: [
          { label: 'Catalogo', path: '/catalog' },
          { label: 'Chi Siamo', path: '/about' },
        ],
      },
      {
        title: 'Legale',
        links: [
          { label: 'Termini e Condizioni', path: '/terms' },
          { label: 'Privacy Policy', path: '/privacy' },
          { label: 'Cookie Policy', path: '/cookie-policy' },
        ],
      },
    ],
    copyright: `© ${new Date().getFullYear()} MorWalPiz Shop`,
    legalLinks: [
      { label: 'Termini', path: '/terms' },
      { label: 'Privacy', path: '/privacy' },
    ],
  },
};
```

---

## 5. Modelli Condivisi (Models)

### 5.1 Estensioni a `@morwalpizvideo/models`

Aggiungere nuovi file TypeScript nel package `frontend/fe-packages/models/src/`:

#### `digitalProduct.ts`

```typescript
export interface DigitalProduct {
  id: string;
  name: string;
  description: string;
  previewImageUrl: string;
  contentStorageKey: string;
  categoryIds: string[];
  price?: number;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface ProductCategory {
  id: string;
  name: string;
  description: string;
  displayOrder?: number;
}
```

#### `customer.ts`

```typescript
export interface Customer {
  id: string;
  email: string;
  name?: string;
  createdAt: Date;
  lastLoginAt?: Date;
  newsletterAccepted: boolean;
  termsAccepted: boolean;
  termsAcceptedAt?: Date;
}

export interface EmailLoginRequest {
  email: string;
  termsAccepted: boolean;
  recaptchaToken: string;
}

export interface EmailLoginResponse {
  customerId: string;
  email: string;
  sessionToken: string;
}
```

#### `cart.ts`

```typescript
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
```

#### `legal.ts`

```typescript
export interface LegalContent {
  type: 'terms' | 'privacy' | 'cookie-policy';
  content: string;
  lastUpdated: Date;
}
```

### 5.2 Export da `index.ts`

```typescript
// frontend/fe-packages/models/src/index.ts
export * from './digitalProduct';
export * from './customer';
export * from './cart';
export * from './legal';
// ... existing exports
```

---

## 6. Servizi Condivisi (Services)

### 6.1 Estensioni a `@morwalpizvideo/services`

#### Endpoint Constants

```typescript
// frontend/fe-packages/services/src/endpoints.ts
export const ENDPOINTS = {
  // ... existing endpoints
  
  // Auth
  AUTH_EMAIL_LOGIN: '/api/auth/email-login',
  
  // Catalog
  CATALOG_PRODUCTS: '/api/catalog/products',
  CATALOG_PRODUCT_BY_ID: (id: string) => `/api/catalog/products/${id}`,
  CATALOG_CATEGORIES: '/api/catalog/categories',
  
  // Cart
  CART_CURRENT: '/api/cart/current',
  CART_ADD_ITEM: '/api/cart/items',
  CART_REMOVE_ITEM: (productId: string) => `/api/cart/items/${productId}`,
  CART_CHECKOUT: '/api/cart/checkout',
  
  // Legal
  LEGAL_TERMS: '/api/legal/terms',
  LEGAL_PRIVACY: '/api/legal/privacy',
  LEGAL_COOKIES: '/api/legal/cookie-policy',
};
```

#### Service Functions

```typescript
// frontend/fe-packages/services/src/catalogService.ts
import { get } from './apiService';
import { ENDPOINTS } from './endpoints';
import type { DigitalProduct, ProductCategory } from '@morwalpizvideo/models';

export async function fetchProducts(): Promise<DigitalProduct[]> {
  return get<DigitalProduct[]>(ENDPOINTS.CATALOG_PRODUCTS);
}

export async function fetchProductById(id: string): Promise<DigitalProduct> {
  return get<DigitalProduct>(ENDPOINTS.CATALOG_PRODUCT_BY_ID(id));
}

export async function fetchCategories(): Promise<ProductCategory[]> {
  return get<ProductCategory[]>(ENDPOINTS.CATALOG_CATEGORIES);
}
```

```typescript
// frontend/fe-packages/services/src/authService.ts
import { post } from './apiService';
import { ENDPOINTS } from './endpoints';
import type { EmailLoginRequest, EmailLoginResponse } from '@morwalpizvideo/models';

export async function emailLogin(request: EmailLoginRequest): Promise<EmailLoginResponse> {
  return post<EmailLoginResponse>(ENDPOINTS.AUTH_EMAIL_LOGIN, request);
}
```

```typescript
// frontend/fe-packages/services/src/cartService.ts
import { get, post, Delete } from './apiService';
import { ENDPOINTS } from './endpoints';
import type { Cart, AddToCartRequest } from '@morwalpizvideo/models';

export async function getCurrentCart(): Promise<Cart> {
  return get<Cart>(ENDPOINTS.CART_CURRENT);
}

export async function addCartItem(request: AddToCartRequest): Promise<Cart> {
  return post<Cart>(ENDPOINTS.CART_ADD_ITEM, request);
}

export async function removeCartItem(productId: string): Promise<Cart> {
  return Delete<Cart>(ENDPOINTS.CART_REMOVE_ITEM(productId));
}

export async function checkoutCart(): Promise<Cart> {
  return post<Cart>(ENDPOINTS.CART_CHECKOUT, {});
}
```

### 6.2 Export da `index.ts`

```typescript
// frontend/fe-packages/services/src/index.ts
export * from './catalogService';
export * from './authService';
export * from './cartService';
// ... existing exports
```

---

## 7. Entità e Relazioni Backend

### 7.1 Domain Models (.NET)

#### `DigitalProduct.cs`

```csharp
// MorWalPizVideo.Models/Models/DigitalProduct.cs
public record DigitalProduct(
    string Id,
    string Name,
    string Description,
    string PreviewImageUrl,
    string ContentStorageKey,
    List<string> CategoryIds,
    decimal? Price,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

#### `Customer.cs`

```csharp
// MorWalPizVideo.Models/Models/Customer.cs
public record Customer(
    string Id,
    string Email,
    string? Name,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    bool NewsletterAccepted,
    bool TermsAccepted,
    DateTime? TermsAcceptedAt
);
```

#### `ProductCategory.cs`

```csharp
// MorWalPizVideo.Models/Models/ProductCategory.cs
public record ProductCategory(
    string Id,
    string Name,
    string Description,
    int? DisplayOrder
);
```

#### `Cart.cs`

```csharp
// MorWalPizVideo.Models/Models/Cart.cs
public record Cart(
    string Id,
    string CustomerId,
    List<CartItem> Items,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt
);

public record CartItem(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal? Price
);
```

### 7.2 MongoDB Collections

```
MongoDB Database: MorWalPizVideoDB
Collections:
  - DigitalProducts
  - Customers
  - ProductCategories
  - Carts
```

### 7.3 Relazioni

```
DigitalProduct (1) ----< (N) ProductCategory
Customer (1) ----< (N) Cart
Cart (1) ----< (N) CartItem --> DigitalProduct
```

---

## 8. Backend API Implementation

### 8.1 ServerAPI Endpoints (BFF/Gateway)

#### `AuthController.cs`

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IRecaptchaValidator _recaptchaValidator;
    
    [HttpPost("email-login")]
    public async Task<ActionResult<EmailLoginResponse>> EmailLogin(
        [FromBody] EmailLoginRequest request)
    {
        // Validate reCAPTCHA
        var isValid = await _recaptchaValidator.ValidateAsync(request.RecaptchaToken);
        if (!isValid)
            return BadRequest("Invalid reCAPTCHA");
        
        // Validate email format
        if (!EmailValidator.IsValid(request.Email))
            return BadRequest("Invalid email format");
        
        // Process login through BackOffice service
        var response = await _customerService.AuthenticateByEmailAsync(request);
        
        return Ok(response);
    }
}
```

#### `CatalogController.cs`

```csharp
[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly IProductService _productService;
    
    [HttpGet("products")]
    public async Task<ActionResult<List<DigitalProduct>>> GetProducts()
    {
        var products = await _productService.GetActiveProductsAsync();
        return Ok(products);
    }
    
    [HttpGet("products/{id}")]
    public async Task<ActionResult<DigitalProduct>> GetProductById(string id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound();
        return Ok(product);
    }
    
    [HttpGet("categories")]
    public async Task<ActionResult<List<ProductCategory>>> GetCategories()
    {
        var categories = await _productService.GetCategoriesAsync();
        return Ok(categories);
    }
}
```

#### `CartController.cs`

```csharp
[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    
    [HttpGet("current")]
    public async Task<ActionResult<Cart>> GetCurrentCart()
    {
        var customerId = User.FindFirst("customer_id")?.Value;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        
        var cart = await _cartService.GetOrCreateCartAsync(customerId);
        return Ok(cart);
    }
    
    [HttpPost("items")]
    public async Task<ActionResult<Cart>> AddItem(
        [FromBody] AddToCartRequest request)
    {
        var customerId = User.FindFirst("customer_id")?.Value;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        
        var cart = await _cartService.AddItemAsync(customerId, request);
        return Ok(cart);
    }
    
    [HttpDelete("items/{productId}")]
    public async Task<ActionResult<Cart>> RemoveItem(string productId)
    {
        var customerId = User.FindFirst("customer_id")?.Value;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        
        var cart = await _cartService.RemoveItemAsync(customerId, productId);
        return Ok(cart);
    }
    
    [HttpPost("checkout")]
    public async Task<ActionResult<Cart>> Checkout()
    {
        var customerId = User.FindFirst("customer_id")?.Value;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        
        var cart = await _cartService.CheckoutAsync(customerId);
        return Ok(cart);
    }
}
```

#### `LegalController.cs`

```csharp
[ApiController]
[Route("api/legal")]
public class LegalController : ControllerBase
{
    private readonly ILegalContentService _legalContentService;
    
    [HttpGet("terms")]
    public async Task<ActionResult<LegalContent>> GetTerms()
    {
        var content = await _legalContentService.GetTermsAsync();
        return Ok(content);
    }
    
    [HttpGet("privacy")]
    public async Task<ActionResult<LegalContent>> GetPrivacy()
    {
        var content = await _legalContentService.GetPrivacyAsync();
        return Ok(content);
    }
    
    [HttpGet("cookie-policy")]
    public async Task<ActionResult<LegalContent>> GetCookiePolicy()
    {
        var content = await _legalContentService.GetCookiePolicyAsync();
        return Ok(content);
    }
}
```

### 8.2 BackOffice Services (Business Logic)

#### `ICustomerService.cs`

```csharp
public interface ICustomerService
{
    Task<EmailLoginResponse> AuthenticateByEmailAsync(EmailLoginRequest request);
    Task<Customer> GetCustomerByEmailAsync(string email);
    Task<Customer> CreateOrUpdateCustomerAsync(string email, bool termsAccepted);
    Task<bool> IsEmailAlreadyRegisteredAsync(string email);
}
```

#### `CustomerService.cs`

```csharp
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly ILogger<CustomerService> _logger;
    
    public async Task<EmailLoginResponse> AuthenticateByEmailAsync(
        EmailLoginRequest request)
    {
        // Normalize email
        var normalizedEmail = NormalizeEmail(request.Email);
        
        // Validate email domain (anti-disposable)
        if (IsDisposableEmailDomain(normalizedEmail))
        {
            _logger.LogWarning("Disposable email attempt: {Email}", normalizedEmail);
            throw new ValidationException("Email domain not allowed");
        }
        
        // Check if customer exists
        var customer = await _repository.GetByEmailAsync(normalizedEmail);
        
        if (customer == null)
        {
            // Create new customer
            customer = new Customer(
                Id: Guid.NewGuid().ToString(),
                Email: normalizedEmail,
                Name: null,
                CreatedAt: DateTime.UtcNow,
                LastLoginAt: DateTime.UtcNow,
                NewsletterAccepted: request.NewsletterAccepted ?? false,
                TermsAccepted: request.TermsAccepted,
                TermsAcceptedAt: DateTime.UtcNow
            );
            
            await _repository.CreateAsync(customer);
            _logger.LogInformation("New customer created: {CustomerId}", customer.Id);
        }
        else
        {
            // Update last login
            customer = customer with 
            { 
                LastLoginAt = DateTime.UtcNow 
            };
            await _repository.UpdateAsync(customer);
            _logger.LogInformation("Customer login: {CustomerId}", customer.Id);
        }
        
        // Generate session token
        var sessionToken = GenerateSessionToken(customer);
        
        return new EmailLoginResponse(
            CustomerId: customer.Id,
            Email: customer.Email,
            SessionToken: sessionToken
        );
    }
    
    private string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
    
    private bool IsDisposableEmailDomain(string email)
    {
        var disposableDomains = new HashSet<string>
        {
            "10minutemail.com",
            "guerrillamail.com",
            "mailinator.com",
            "tempmail.com",
            // ... more disposable domains
        };
        
        var domain = email.Split('@').LastOrDefault();
        return domain != null && disposableDomains.Contains(domain);
    }
    
    private string GenerateSessionToken(Customer customer)
    {
        // Generate JWT or simple token
        // Implementation depends on auth strategy
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
```

#### `IProductService.cs`

```csharp
public interface IProductService
{
    Task<List<DigitalProduct>> GetActiveProductsAsync();
    Task<DigitalProduct?> GetProductByIdAsync(string id);
    Task<List<ProductCategory>> GetCategoriesAsync();
    Task<DigitalProduct> CreateProductAsync(DigitalProduct product);
    Task<DigitalProduct> UpdateProductAsync(DigitalProduct product);
    Task DeleteProductAsync(string id);
}
```

#### `ICartService.cs`

```csharp
public interface ICartService
{
    Task<Cart> GetOrCreateCartAsync(string customerId);
    Task<Cart> AddItemAsync(string customerId, AddToCartRequest request);
    Task<Cart> RemoveItemAsync(string customerId, string productId);
    Task<Cart> CheckoutAsync(string customerId);
    Task<List<Cart>> GetCompletedOrdersAsync(string customerId);
}
```

### 8.3 Repository Pattern

#### `IDigitalProductRepository.cs`

```csharp
public interface IDigitalProductRepository : IRepository<DigitalProduct>
{
    Task<List<DigitalProduct>> GetActiveProductsAsync();
    Task<List<DigitalProduct>> GetProductsByCategoryAsync(string categoryId);
}
```

#### `ICustomerRepository.cs`

```csharp
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
}
```

#### `ICartRepository.cs`

```csharp
public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetActiveCartByCustomerIdAsync(string customerId);
    Task<List<Cart>> GetCompletedCartsByCustomerIdAsync(string customerId);
}
```

---

## 9. Autenticazione e Sicurezza

### 9.1 Flusso di Login

```
User enters email → Frontend validates format → reCAPTCHA verification
     ↓
ServerAPI receives request → Validates reCAPTCHA token → Rate limit check
     ↓
BackOffice authenticates → Normalizes email → Checks disposable domain
     ↓
Customer exists? 
  ├─ Yes: Update LastLoginAt → Generate session token
  └─ No: Create new Customer → Generate session token
     ↓
Return session token → Frontend stores in localStorage
     ↓
Subsequent requests include session token in headers
```

### 9.2 Local Storage Session Management

```typescript
// src/store/auth-storage.ts
const AUTH_STORAGE_KEY = 'morwalpiz_shop_auth';

export interface AuthSession {
  customerId: string;
  email: string;
  sessionToken: string;
  expiresAt: number;
}

export function saveAuthSession(session: AuthSession): void {
  const sessionWithExpiry = {
    ...session,
    expiresAt: Date.now() + (24 * 60 * 60 * 1000), // 24 hours
  };
  localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(sessionWithExpiry));
}

export function getAuthSession(): AuthSession | null {
  const stored = localStorage.getItem(AUTH_STORAGE_KEY);
  if (!stored) return null;
  
  const session = JSON.parse(stored) as AuthSession;
  
  // Check expiration
  if (session.expiresAt < Date.now()) {
    clearAuthSession();
    return null;
  }
  
  return session;
}

export function clearAuthSession(): void {
  localStorage.removeItem(AUTH_STORAGE_KEY);
}

export function isAuthenticated(): boolean {
  return getAuthSession() !== null;
}
```

### 9.3 Email Validation

```typescript
// src/utils/email-validator.ts
const DISPOSABLE_DOMAINS = [
  '10minutemail.com',
  'guerrillamail.com',
  'mailinator.com',
  'tempmail.com',
  // ... more
];

export function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
}

export function normalizeEmail(email: string): string {
  return email.trim().toLowerCase();
}

export function isDisposableEmail(email: string): boolean {
  const domain = email.split('@')[1];
  return DISPOSABLE_DOMAINS.includes(domain);
}

export function validateEmailForRegistration(email: string): {
  valid: boolean;
  error?: string;
} {
  const normalized = normalizeEmail(email);
  
  if (!isValidEmail(normalized)) {
    return { valid: false, error: 'Formato email non valido' };
  }
  
  if (isDisposableEmail(normalized)) {
    return { valid: false, error: 'Email temporanea non consentita' };
  }
  
  return { valid: true };
}
```

### 9.4 reCAPTCHA Integration

```typescript
// src/utils/recaptcha-helper.ts
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';

export function useRecaptcha() {
  const { executeRecaptcha } = useGoogleReCaptcha();
  
  const getRecaptchaToken = async (action: string = 'login'): Promise<string> => {
    if (!executeRecaptcha) {
      throw new Error('reCAPTCHA not ready');
    }
    
    return await executeRecaptcha(action);
  };
  
  return { getRecaptchaToken };
}
```

```typescript
// Usage in LoginForm component
const { getRecaptchaToken } = useRecaptcha();

const handleLogin = async (email: string) => {
  const token = await getRecaptchaToken('email_login');
  
  const response = await emailLogin({
    email,
    termsAccepted: true,
    recaptchaToken: token,
  });
  
  saveAuthSession(response);
};
```

### 9.5 Rate Limiting (Backend)

```csharp
// In ServerAPI Startup/Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// Apply to AuthController
[EnableRateLimiting("auth")]
[HttpPost("email-login")]
public async Task<ActionResult<EmailLoginResponse>> EmailLogin(...)
```

---

## 10. Termini e Condizioni

### 10.1 Contenuto Pagina

```typescript
// src/routes/terms-and-conditions.tsx
export default function TermsAndConditions() {
  return (
    <div className="container my-5">
      <h1>Termini e Condizioni</h1>
      
      <section>
        <h2>1. Accettazione dei Termini</h2>
        <p>
          Utilizzando il servizio MorWalPiz Shop, accetti i seguenti termini e condizioni.
          Se non accetti questi termini, non potrai accedere al servizio.
        </p>
      </section>
      
      <section>
        <h2>2. Utilizzo dell'Email</h2>
        <p>
          La tua email verrà utilizzata esclusivamente per:
        </p>
        <ul>
          <li>Gestione dell'account e autenticazione</li>
          <li>Invio di newsletter (solo se acconsentito)</li>
          <li>Comunicazioni relative agli ordini</li>
        </ul>
        <p>
          <strong>La tua email NON verrà mai ceduta a terzi.</strong>
        </p>
      </section>
      
      <section>
        <h2>3. Newsletter</h2>
        <p>
          Se hai acconsentito all'invio di newsletter, potrai disiscriverti in qualsiasi momento
          utilizzando il link presente in ogni email.
        </p>
      </section>
      
      <section>
        <h2>4. Contenuti Digitali</h2>
        <p>
          I contenuti digitali acquistati sono di tua proprietà personale e non possono essere
          ridistribuiti o rivenduti senza autorizzazione esplicita.
        </p>
      </section>
      
      <section>
        <h2>5. Privacy e Protezione Dati</h2>
        <p>
          I tuoi dati personali sono protetti secondo le normative GDPR. Per maggiori informazioni,
          consulta la nostra <a href="/privacy">Privacy Policy</a>.
        </p>
      </section>
      
      <section>
        <h2>6. Modifiche ai Termini</h2>
        <p>
          Ci riserviamo il diritto di modificare questi termini in qualsiasi momento. 
          Le modifiche saranno effettive dalla data di pubblicazione sul sito.
        </p>
      </section>
      
      <p className="text-muted mt-5">
        Ultimo aggiornamento: {new Date().toLocaleDateString('it-IT')}
      </p>
    </div>
  );
}
```

### 10.2 Checkbox nel Login Form

```typescript
// src/components/LoginForm.tsx
const [termsAccepted, setTermsAccepted] = useState(false);

<div className="form-check">
  <input
    className="form-check-input"
    type="checkbox"
    id="termsCheck"
    checked={termsAccepted}
    onChange={(e) => setTermsAccepted(e.target.checked)}
    required
  />
  <label className="form-check-label" htmlFor="termsCheck">
    Accetto i{' '}
    <a href="/terms" target="_blank" rel="noopener noreferrer">
      Termini e Condizioni
    </a>
  </label>
</div>

<button
  type="submit"
  disabled={!termsAccepted || !email}
  className="btn btn-primary"
>
  Accedi
</button>
```

---

## 11. Backoffice UI Extension

### 11.1 Nuove Route in `back-office-spa`

```typescript
// src/router/routes/index.ts
import DigitalProducts from '@/routes/digitalProducts';
import ProductCategories from '@/routes/productCategories';
import Customers from '@/routes/customers';
import Carts from '@/routes/carts';

// Add to router configuration
{
  path: 'digital-products',
  children: [
    { index: true, loader: DigitalProducts.loader, Component: DigitalProducts.List },
    { path: 'create', Component: DigitalProducts.Form },
    { path: ':id', loader: DigitalProducts.detailLoader, Component: DigitalProducts.Detail },
    { path: ':id/edit', loader: DigitalProducts.formLoader, Component: DigitalProducts.Form },
  ],
},
{
  path: 'product-categories',
  children: [
    { index: true, loader: ProductCategories.loader, Component: ProductCategories.List },
    { path: 'create', Component: ProductCategories.Form },
    { path: ':id/edit', loader: ProductCategories.formLoader, Component: ProductCategories.Form },
  ],
},
{
  path: 'customers',
  children: [
    { index: true, loader: Customers.loader, Component: Customers.List },
    { path: ':id', loader: Customers.detailLoader, Component: Customers.Detail },
  ],
},
{
  path: 'carts',
  children: [
    { index: true, loader: Carts.loader, Component: Carts.List },
    { path: ':id', loader: Carts.detailLoader, Component: Carts.Detail },
  ],
},
```

### 11.2 CRUD Components

#### Digital Products List

```typescript
// src/routes/digitalProducts/index/Component.tsx
export default function DigitalProductsList() {
  const { products } = useLoaderData<{ products: DigitalProduct[] }>();
  
  return (
    <div className="container">
      <h1>Prodotti Digitali</h1>
      <Link to="create" className="btn btn-primary mb-3">
        Crea Nuovo Prodotto
      </Link>
      
      <table className="table">
        <thead>
          <tr>
            <th>Nome</th>
            <th>Categorie</th>
            <th>Attivo</th>
            <th>Azioni</th>
          </tr>
        </thead>
        <tbody>
          {products.map(product => (
            <tr key={product.id}>
              <td>{product.name}</td>
              <td>{product.categoryIds.length}</td>
              <td>
                <span className={`badge ${product.isActive ? 'bg-success' : 'bg-secondary'}`}>
                  {product.isActive ? 'Attivo' : 'Inattivo'}
                </span>
              </td>
              <td>
                <Link to={product.id} className="btn btn-sm btn-info me-2">
                  Dettagli
                </Link>
                <Link to={`${product.id}/edit`} className="btn btn-sm btn-warning">
                  Modifica
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

#### Customer List

```typescript
// src/routes/customers/index/Component.tsx
export default function CustomersList() {
  const { customers } = useLoaderData<{ customers: Customer[] }>();
  
  return (
    <div className="container">
      <h1>Clienti</h1>
      
      <table className="table">
        <thead>
          <tr>
            <th>Email</th>
            <th>Registrato</th>
            <th>Ultimo Accesso</th>
            <th>Newsletter</th>
            <th>Azioni</th>
          </tr>
        </thead>
        <tbody>
          {customers.map(customer => (
            <tr key={customer.id}>
              <td>{customer.email}</td>
              <td>{new Date(customer.createdAt).toLocaleDateString('it-IT')}</td>
              <td>
                {customer.lastLoginAt 
                  ? new Date(customer.lastLoginAt).toLocaleDateString('it-IT')
                  : 'Mai'
                }
              </td>
              <td>
                {customer.newsletterAccepted ? (
                  <span className="badge bg-success">Sì</span>
                ) : (
                  <span className="badge bg-secondary">No</span>
                )}
              </td>
              <td>
                <Link to={customer.id} className="btn btn-sm btn-info">
                  Dettagli
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

---

## 12. Roadmap di Implementazione

**STATO AGGIORNAMENTO**: 25 Marzo 2026, 10:58 AM

### Fase 1: Fondamenta (2 settimane) ✅ **COMPLETATA**

#### Settimana 1: Package Layout ✅
- [x] Creare struttura `frontend/fe-packages/layout/morwalpiz-layout`
- [x] Implementare componenti base (AppShell, SiteHeader, SiteFooter)
- [x] Definire interfacce TypeScript per configurazione
- [x] Implementare stili SCSS parametrici
- [x] Compilare e testare package
- [x] Aggiornare workspace root `package.json`

**Note**: Package completato con componenti Layout parametrici (AppShell, SiteHeader, SiteFooter), type definitions complete, e stili SCSS con supporto social media. Build TypeScript riuscita.

#### Settimana 2: App Scaffold 🔄 **IN ATTESA**
- [ ] Creare struttura `frontend/morwalpiz-shop.client`
- [ ] Configurare Vite, TypeScript, ESLint, Prettier
- [ ] Implementare `main.tsx` e routing base
- [ ] Creare componente `Root` con layout parametrico
- [ ] Implementare route pubbliche (home, login, error)
- [ ] Configurare Docker e nginx

### Fase 2: Modelli e Servizi (1 settimana) ✅ **COMPLETATA**

- [x] Estendere `@morwalpizvideo/models` con nuovi tipi
- [x] Aggiungere export da `index.ts`
- [x] Rebuild package models
- [x] Estendere `@morwalpizvideo/services` con nuovi endpoint
- [x] Implementare service functions (catalog, auth, cart)
- [x] Rebuild package services ✅ **Build riuscita senza errori**
- [x] Fix TypeScript type parameter issues in shopService.ts
- [ ] Testare integrazione nei due package consumer

**Modelli Implementati**:
- `digitalProduct.ts`: DigitalProduct, ProductCategory, Create/Update DTOs
- `customer.ts`: Customer, EmailLoginRequest, EmailVerificationRequest, LoginResponse
- `cart.ts`: Cart, CartItem, AddToCartRequest, UpdateCartItemRequest, CheckoutRequest/Response
- `legal.ts`: LegalContent, LegalContentType, Create/Update DTOs

**Servizi Implementati**:
- `endpoints.ts`: Aggiunti endpoint shop (SHOP_PRODUCTS, SHOP_AUTH_LOGIN, SHOP_CART, SHOP_LEGAL, etc.)
- `shopService.ts`: Service functions complete per digital products, auth, cart, legal content
- Tutti i servizi esportati da `index.ts`

**Stato Build** (Aggiornato 25/03/2026):
- ✅ `@morwalpizvideo/models`: Build riuscita con tutti i nuovi types
- ✅ `@morwalpiz/layout`: Build riuscita (TypeScript + SCSS compilation)
- ✅ `@morwalpizvideo/services`: Build riuscita dopo fix type parameters

### Fase 3: Autenticazione (1 settimana) 🔄 **IN CORSO**

**Frontend** ✅ **Completato**:
- [x] Creare utility `auth-storage.ts` per local storage
- [x] Implementare validazione email frontend (`email-validator.ts`)
- [x] Creare utility `recaptcha-helper.ts` per reCAPTCHA integration
- [x] Creare `AuthContext.tsx` per React context
- [x] Creare route `/login` (login.tsx)
- [x] Implementare layout configuration (layout-config.ts)

**Backend** ⏳ **Da Implementare**:
- [ ] Implementare `AuthController` in ServerAPI
- [ ] Implementare `CustomerService` in BackOffice
- [ ] Implementare repository `CustomerRepository`
- [ ] Aggiungere rate limiting
- [ ] Testare flusso end-to-end

**Componenti Mancanti**:
- [ ] `LoginForm` component con validazione email e reCAPTCHA UI

### Fase 4: Catalogo Prodotti (2 settimane)

#### Frontend
- [ ] Implementare route `/catalog`
- [ ] Creare componenti `ProductCard`, `ProductGrid`
- [ ] Implementare filtri per categoria
- [ ] Implementare route `/products/:id` per dettaglio
- [ ] Integrare preview da Azure Blob Storage

#### Backend
- [ ] Creare modelli .NET per `DigitalProduct`, `ProductCategory`
- [ ] Implementare `CatalogController` in ServerAPI
- [ ] Implementare `ProductService` in BackOffice
- [ ] Implementare repository `DigitalProductRepository`
- [ ] Creare collection MongoDB
- [ ] Testare CRUD prodotti

### Fase 5: Carrello (1 settimana)

- [ ] Implementare route `/cart`
- [ ] Creare componente `CartSummary`
- [ ] Implementare store locale carrello (`cart-storage.ts`)
- [ ] Implementare `CartController` in ServerAPI
- [ ] Implementare `CartService` in BackOffice
- [ ] Implementare repository `CartRepository`
- [ ] Testare add/remove item
- [ ] Implementare checkout

### Fase 6: Pagine Legali (3 giorni)

- [ ] Implementare route `/terms`
- [ ] Implementare route `/privacy`
- [ ] Implementare route `/cookie-policy`
- [ ] Creare contenuto testuale
- [ ] Implementare `LegalController` in ServerAPI
- [ ] Testare visualizzazione

### Fase 7: Backoffice UI (1 settimana)

- [ ] Creare route prodotti digitali in `back-office-spa`
- [ ] Implementare lista, dettaglio, form prodotti
- [ ] Creare route categorie prodotto
- [ ] Implementare lista, form categorie
- [ ] Creare route clienti
- [ ] Implementare lista, dettaglio clienti
- [ ] Creare route carrelli/ordini
- [ ] Implementare lista, dettaglio ordini

### Fase 8: Testing & Hardening (1 settimana)

- [ ] Test end-to-end flusso completo
- [ ] Verificare validazioni email
- [ ] Testare rate limiting
- [ ] Verificare sicurezza reCAPTCHA
- [ ] Test performance caricamento catalogo
- [ ] Verificare persistenza carrello
- [ ] Test mobile responsiveness
- [ ] Audit sicurezza
- [ ] Code review completo

### Fase 9: Deployment (3 giorni)

- [ ] Configurare environment production
- [ ] Deploy ServerAPI su Azure
- [ ] Deploy BackOffice su Azure
- [ ] Build Docker image `morwalpiz-shop.client`
- [ ] Deploy container su Azure
- [ ] Configurare DNS e certificati SSL
- [ ] Monitoring e logging
- [ ] Smoke test production

---

## 13. Rischi e Mitigazioni

### 13.1 Rischi Tecnici

| Rischio | Probabilità | Impatto | Mitigazione |
|---------|-------------|---------|-------------|
| Complessità generalizzazione layout | Media | Medio | Iniziare con caso d'uso semplice, iterare |
| Breaking changes in package condivisi | Bassa | Alto | Versionamento semantico, test regressione |
| Integrazione Azure Blob Storage | Bassa | Medio | Usare SDK ufficiale, implementare fallback |
| Performance caricamento catalogo | Media | Medio | Paginazione, caching, lazy loading |
| Attacchi spam email | Alta | Medio | reCAPTCHA, rate limiting, blacklist domini |

### 13.2 Rischi Operativi

| Rischio | Probabilità | Impatto | Mitigazione |
|---------|-------------|---------|-------------|
| Sovrapposizione con sviluppo esistente | Media | Basso | Comunicazione frequente team |
| Carenza documentazione | Media | Medio | Documentare durante sviluppo |
| Testing insufficiente | Media | Alto | TDD, code coverage >80% |
| Deploy fallito | Bassa | Alto | Blue-green deployment, rollback plan |

### 13.3 Rischi Business

| Rischio | Probabilità | Impatto | Mitigazione |
|---------|-------------|---------|-------------|
| Cambio requisiti | Media | Medio | Architettura modulare, refactoring safe |
| GDPR compliance | Bassa | Alto | Audit legale, documentazione chiara |
| Abuso sistema email fake | Alta | Medio | Validazioni multiple, monitoring |

---

## 14. Metriche di Successo

### 14.1 KPI Tecnici

- **Build Time**: < 2 minuti per frontend apps
- **Bundle Size**: < 500KB gzipped per morwalpiz-shop.client
- **Page Load Time**: < 2 secondi (First Contentful Paint)
- **API Response Time**: < 300ms (p95)
- **Test Coverage**: > 80% per business logic

### 14.2 KPI Funzionali

- **Login Success Rate**: > 95%
- **Email Validation Accuracy**: > 99%
- **Spam Email Block Rate**: > 90%
- **Cart Abandonment**: < 30%
- **Checkout Completion**: > 70%

### 14.3 KPI Operativi

- **Deployment Frequency**: 2+ volte/settimana
- **Mean Time to Recovery (MTTR)**: < 1 ora
- **Incident Rate**: < 1 critical/settimana
- **Code Review Time**: < 24 ore

---

## 15. Decisioni Architetturali Chiave

### ADR-001: Package Layout Condiviso
**Contesto**: Evitare duplicazione codice layout tra applicazioni  
**Decisione**: Creare `@morwalpiz/layout` come package condiviso parametrico  
**Conseguenze**: (+) Riuso, (-) Complessità iniziale, (+) Manutenibilità long-term

### ADR-002: Local Storage per Sessione Cliente
**Contesto**: Autenticazione semplice senza password  
**Decisione**: Usare localStorage per session token  
**Conseguenze**: (+) Semplicità, (-) Sicurezza limitata, (!) Session timeout necessario

### ADR-003: ServerAPI come BFF
**Contesto**: Separare frontend concerns da business logic  
**Decisione**: ServerAPI gestisce solo validazione e mapping, BackOffice gestisce domain  
**Conseguenze**: (+) Separation of concerns, (+) Testabilità, (-) Network hops aggiuntivi

### ADR-004: Email-only Authentication
**Contesto**: Semplificare UX eliminando password  
**Decisione**: Login tramite email + reCAPTCHA, no password  
**Conseguenze**: (+) UX migliore, (-) Sicurezza ridotta, (!) Rate limiting critico

### ADR-005: Azure Blob Storage per Contenuti
**Contesto**: Gestire asset digitali scalabili  
**Decisione**: Usare Azure Blob Storage con reference in MongoDB  
**Conseguenze**: (+) Scalabilità, (+) CDN ready, (-) Costi storage

---

## 16. Glossario

- **BFF**: Backend For Frontend, layer API specifico per client frontend
- **SPA**: Single Page Application
- **reCAPTCHA**: Sistema Google per protezione anti-bot
- **Local Storage**: Storage browser-side per persistenza client
- **Blob Storage**: Storage cloud per file binari (Azure)
- **DTO**: Data Transfer Object, contratto API
- **Domain Model**: Rappresentazione entità business
- **Repository Pattern**: Pattern accesso dati astratto
- **Rate Limiting**: Limitazione richieste per prevenire abusi
- **Disposable Email**: Email temporanea/usa-e-getta

---

## 17. Riferimenti

### Documentazione Tecnica
- [React Router v7 Documentation](https://reactrouter.com)
- [Vite Documentation](https://vitejs.dev)
- [Azure Blob Storage .NET SDK](https://docs.microsoft.com/azure/storage/blobs/)
- [Google reCAPTCHA v3](https://developers.google.com/recaptcha/docs/v3)

### Codice Esistente
- `frontend/morwalpizvideo.client` - App di riferimento per layout
- `frontend/fe-packages/models` - Package modelli esistente
- `frontend/fe-packages/services` - Package servizi esistente
- `MorWalPizVideo.ServerAPI` - BFF/Gateway esistente
- `MorWalPizVideo.BackOffice` - Business logic esistente

### Standard e Best Practices
- [React Best Practices 2026](https://react.dev/learn)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [ASP.NET Core Best Practices](https://docs.microsoft.com/aspnet/core/)
- [MongoDB Schema Design](https://www.mongodb.com/docs/manual/core/data-modeling-introduction/)

---

## 18. Appendice: Esempio Completo Flow

### User Story: "Acquisto Documento Digitale"

1. **Utente accede al sito**
   - URL: `https://shop.morwalpiz.com`
   - Viene caricata la home page con layout parametrico

2. **Utente naviga al catalogo**
   - Click su "Catalogo" nel menu header
   - GET `/api/catalog/products`
   - Visualizza griglia prodotti con preview

3. **Utente seleziona prodotto**
   - Click su card prodotto
   - GET `/api/catalog/products/{id}`
   - Visualizza dettaglio con descrizione completa

4. **Utente aggiunge al carrello** (non autenticato)
   - Click "Aggiungi al Carrello"
   - Redirect a `/login`

5. **Utente esegue login**
   - Inserisce email
   - Accetta termini e condizioni
   - reCAPTCHA automatico
   - POST `/api/auth/email-login`
   - Backend: valida email, crea/aggiorna customer, genera token
   - Token salvato in localStorage
   - Redirect a `/catalog` o back to product

6. **Utente aggiunge al carrello** (autenticato)
   - POST `/api/cart/items` con productId
   - Backend: recupera cart attivo o crea nuovo
   - Aggiunge item
   - Visualizza badge carrello aggiornato

7. **Utente visualizza carrello**
   - Click su icona carrello
   - GET `/api/cart/current`
   - Visualizza lista prodotti, quantità, totale

8. **Utente completa ordine**
   - Click "Checkout"
   - POST `/api/cart/checkout`
   - Backend: imposta `isCompleted = true`, salva timestamp
   - Redirect a `/checkout-success`

9. **Utente accede ai contenuti acquistati**
   - Contenuti disponibili tramite riferimento localStorage
   - Link download da Azure Blob Storage con SAS token temporaneo

---

**Fine del Piano di Implementazione**

*Documento redatto il 25 Marzo 2026*  
*Versione 1.0*  
*Autore: Cline AI Assistant*