// Components
export { AppShell } from './components/AppShell.js';
export { SiteHeader } from './components/SiteHeader.js';
export { SiteFooter } from './components/SiteFooter.js';

// Pepperbox components (feature 002)
export { EmptyState } from './components/EmptyState.js';
export { VideoCard } from './components/VideoCard.js';
export { VideoCardRail } from './components/VideoCardRail.js';
export { HeroCarousel } from './components/HeroCarousel.js';
export { PepperboxSidebar } from './components/PepperboxSidebar.js';
export { PepperboxTopBar } from './components/PepperboxTopBar.js';
export { CategoryBanner } from './components/CategoryBanner.js';
export { CategoryVideoRow } from './components/CategoryVideoRow.js';
export { VideoPlayer } from './components/VideoPlayer.js';

// Utils
export { prefersReducedMotion, onPrefersReducedMotionChange } from './utils/prefersReducedMotion.js';
export { formatRelativeTime } from './utils/formatRelativeTime.js';

// Types
export type {
  LayoutConfig,
  BrandConfig,
  HeaderConfig,
  FooterConfig,
  NavItem,
  SocialItem,
  LegalPageLink,
  FooterSection,
  ThemeConfig,
} from './types.js';

// Component Props
export type { AppShellProps } from './components/AppShell.js';
export type { SiteHeaderProps } from './components/SiteHeader.js';
export type { SiteFooterProps } from './components/SiteFooter.js';
export type { EmptyStateProps } from './components/EmptyState.js';
export type { VideoCardProps, VideoCardChannel } from './components/VideoCard.js';
export type { VideoCardRailProps } from './components/VideoCardRail.js';
export type { HeroCarouselProps, HeroSlide } from './components/HeroCarousel.js';
export type { PepperboxSidebarProps, SidebarNavItem, SidebarFooterLink } from './components/PepperboxSidebar.js';
export type { PepperboxTopBarProps } from './components/PepperboxTopBar.js';
export type { CategoryBannerProps } from './components/CategoryBanner.js';
export type { CategoryVideoRowProps } from './components/CategoryVideoRow.js';
export type { VideoPlayerProps } from './components/VideoPlayer.js';
