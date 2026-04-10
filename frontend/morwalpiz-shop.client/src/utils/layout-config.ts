import { LayoutConfig } from '@morwalpiz/layout';

/**
 * Layout Configuration for MorWalPiz Shop
 * 
 * Note: Navigation items are static. For dynamic auth-aware navigation,
 * update the navigation prop based on auth state in the Root component.
 */
export const layoutConfig: LayoutConfig = {
  brand: {
    name: 'MorWalPiz Shop',
    logo: '/logo.png',
    tagline: 'Documenti Digitali',
    homeRoute: '/',
  },
  header: {
    navigation: [
      { label: 'Home', path: '/' },
      { label: 'Catalogo', path: '/catalog' },
      { label: 'Carrello', path: '/cart' },
      { label: 'Login', path: '/login' },
    ],
    socialLinks: [
      {
        platform: 'telegram',
        url: 'https://t.me/morwalpiz',
        label: 'Telegram',
      },
      {
        platform: 'instagram',
        url: 'https://instagram.com/morwalpiz',
        label: 'Instagram',
      },
    ],
    showBrand: true,
  },
  footer: {
    sections: [
      {
        title: 'Pagine',
        links: [
          { label: 'Home', path: '/' },
          { label: 'Catalogo', path: '/catalog' },
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
    copyright: `© ${new Date().getFullYear()} MorWalPiz Shop. Tutti i diritti riservati.`,
    legalLinks: [
      { label: 'Termini', path: '/terms' },
      { label: 'Privacy', path: '/privacy' },
      { label: 'Cookie', path: '/cookie-policy' },
    ],
  },
};