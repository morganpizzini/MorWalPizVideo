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
  dimensions?: string;
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