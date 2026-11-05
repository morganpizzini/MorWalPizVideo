---
name: MorWalPizVideo Client
description: A creator hub with an editorial video, series, article, and community homepage.
colors:
  magazine-bg: '#101010'
  magazine-surface: '#181818'
  magazine-elevated: '#222222'
  magazine-accent: '#ffff36'
  magazine-text: '#ffffff'
  magazine-secondary: '#b8b8b8'
  magazine-border: '#383838'
  magazine-accent-hover: '#eded2f'
  youtube-hover: '#ef4f5f'
  magazine-shadow-soft: 'rgba(0, 0, 0, 0.1)'
  magazine-shadow-medium: 'rgba(0, 0, 0, 0.15)'
  magazine-shadow-toolbar: 'rgba(0, 0, 0, 0.2)'
  magazine-shadow-quick-links-rest: 'rgba(0, 0, 0, 0.22)'
  magazine-shadow: 'rgba(0, 0, 0, 0.24)'
  magazine-shadow-strong: 'rgba(0, 0, 0, 0.3)'
  magazine-shadow-control: 'rgba(0, 0, 0, 0.35)'
  magazine-shadow-hover: 'rgba(0, 0, 0, 0.36)'
  magazine-shadow-popover: 'rgba(0, 0, 0, 0.4)'
  magazine-shadow-overlay: 'rgba(0, 0, 0, 0.82)'
  magazine-shadow-quick-links: 'rgba(0, 0, 0, 0.32)'
  magazine-shadow-focus: 'rgba(255, 255, 54, 0.2)'
  social-gradient-start: '#edb527'
  social-gradient-end: '#863eae'
  print-border: '#dee2e6'
  accent-hover: '#e6e626'
  shorts: '#df1010'
  donation-border: '#878727'
  collection-border: '#74743b'
  telegram: '#0088b8'
  instagram: '#d02b70'
  youtube: '#e00000'
  discord: '#5865f2'
typography:
  body:
    fontFamily: 'Manrope, Roboto, sans-serif'
    fontSize: '14px'
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: '0'
  heading:
    fontFamily: 'Manrope, Roboto, sans-serif'
    fontWeight: 800
  label:
    fontFamily: 'Manrope, Roboto, sans-serif'
    fontSize: '12px'
    fontWeight: 700
  body-large:
    fontSize: '16px'
  small:
    fontSize: '12px'
  title:
    fontSize: '20px'
  section:
    fontSize: '24px'
  page:
    fontSize: '32px'
  feature:
    fontSize: '40px'
  display:
    fontSize: '56px'
  compact-brand:
    fontSize: '19px'
  brand:
    fontSize: '23px'
  social-icon:
    fontSize: '22px'
  footer:
    fontSize: '13px'
  home:
    fontSize: '36px'
  home-supporting:
    fontSize: '15px'
  home-heading:
    fontSize: '30px'
  editorial-heading:
    fontSize: '22px'
  card-title:
    fontSize: '18px'
  micro:
    fontSize: '10px'
  micro-large:
    fontSize: '11px'
  home-featured:
    fontSize: '28px'
  home-featured-compact:
    fontSize: '26px'
  accessory-heading:
    fontSize: '1.35rem'
  accessory-subheading:
    fontSize: '0.8rem'
  accessory-description:
    fontSize: '0.82rem'
  calendar-heading-mobile:
    fontSize: '1.1rem'
  calendar-description:
    fontSize: '0.95rem'
  quick-link-arrow:
    fontSize: '1.2rem'
rounded:
  sm: '4px'
  md: '8px'
  lg: '10px'
  pill: '999px'
  circle: '50%'
  button: '24px'
  filter: '20px'
  media: '7px'
  badge: '5px'
  marker: '1px'
  quick-link-icon: '0.35rem'
  calendar: '12px'
  calendar-action: '6px'
  accessory: '0.6rem'
  card: '0.375rem'
spacing:
  sm: '8px'
  md: '16px'
  lg: '30px'
components:
  editorial-button:
    backgroundColor: '{colors.magazine-accent}'
    textColor: '{colors.magazine-bg}'
    rounded: '24px'
    minHeight: '44px'
  content-card:
    backgroundColor: '{colors.magazine-surface}'
    textColor: '{colors.magazine-text}'
    rounded: '{rounded.md}'
  filter:
    backgroundColor: 'transparent'
    selectedColor: '{colors.magazine-accent}'
    rounded: '20px'
---

# Design System: MorWalPizVideo Client

## Overview

**Creative North Star: "Digital magazine for the range"**

The public site is a dark editorial hub for Morgan Pizzini's videos, series,
articles, gear, sponsors, and community. It is designed for quick scanning first,
with strong media, concise metadata, and a yellow signal accent. Every registered
public route shares this visual system and site chrome while retaining a
task-specific content hierarchy.

## Colors

- **Magazine black** (`#101010`) is the page background and header/footer base.
- **Magazine surface** (`#181818`) frames panels and cards.
- **Magazine elevated** (`#222222`) supports alerts, media placeholders, and menus.
- **Signal yellow** (`#ffff36`) identifies primary actions, selected filters, and active navigation.
- **White** (`#ffffff`) is reserved for primary content and link text.
- **Secondary gray** (`#b8b8b8`) is used for supporting text and metadata.
- **Border gray** (`#383838`) separates dense editorial regions.
- Social colors identify their destination only: Telegram, Instagram, YouTube, and Discord.
- Hover and depth tokens are reserved for interaction feedback and layered surfaces;
  neutral frames keep logo artwork legible without changing the brand palette.

The palette is intentionally high contrast. Yellow is an action and state color,
not a page wash.

## Typography

Manrope is used throughout the public shell. The brand wordmark remains the image
asset in the header. Headings use strong weight and compact line height; supporting
copy stays restrained and wraps naturally rather than clipping.
The base type ramp is exposed as app-level CSS variables; compact labels and
page-specific heading sizes remain named role tokens where preserving their
existing hierarchy requires an intermediate size.

## Layout

The shared content frame is capped at 1540px with 30px desktop gutters and 16px
phone gutters. Route content may use a narrower reading measure or its own
responsive grid. The homepage's featured region combines a wide content panel
with Follow and Donation panels. Its latest feed is a four-column desktop grid,
three columns on tablet, two columns on larger phones, and one column below 480px
so card titles and metadata remain readable. Article cards follow the same rule.
Matches, calendar, sponsors, accessories, forms, gallery, legal pages, and
utility pages keep layouts suited to their individual tasks.

Media keeps stable 16:9 boxes. Filters scroll horizontally within their own row;
the page itself must never acquire horizontal overflow. Safe-area insets are
included in the sticky header and footer for full-screen mobile browsers.

## Interaction

The header becomes a disclosure menu below 1200px. It exposes expanded state,
focuses the first menu item when opened, traps Tab within the open navigation,
closes on Escape or route activation, and returns focus to the toggle. Search,
account, social, filter, share, and editorial controls provide visible focus and
44px coarse-pointer targets. Hover-only media transforms run only for fine pointers.

Reduced-motion preferences remove authored movement while preserving immediate
color, focus, and selected-state feedback. The shared shell uses a non-landmark
content frame as the skip-link target; each rendered route provides exactly one
page-level main landmark, including QuickLinksRenderer-owned pages.

## Components

### Editorial cards

Cards pair an image, content-type badge, title, optional description, destination,
and date/share footer. Series retain an episode-count badge. The first visible
featured image is eager; below-fold images are lazy with stable dimensions.

### Editorial controls

Yellow filled buttons are primary actions. Outline buttons, filter chips, and text
links use the same accent with clear pressed, disabled, hover, and focus-visible
states. Share controls remain compact visually but expand to touch-safe hit areas
for coarse pointers.
Section headings use a narrow yellow leading marker as an intentional editorial
cue; it is not used as a card border.

### App-level notices

Cookie consent and notification prompts use the magazine surface, border, shadow,
and signal-yellow action tokens. Keep fixed notices within the shared 1540px
content frame, include mobile gutters and safe-area insets, and retain 44px
controls. Notices are non-modal and must wrap their copy and actions without
introducing page overflow.

### Header and footer

The sticky header provides the brand, route navigation, search, social destinations,
and authenticated profile action. The footer keeps configured navigation plus stable
Sponsor, Gear, Articles, Donation, Privacy, Terms, and consent destinations.

## Responsive rules

- 1200px: collapse primary navigation into a menu and stack sponsor/gear regions.
- 768px: stack the feature region and reduce page gutters.
- 480px: use one content column, stack Follow and Donation, and preserve full-width controls.
- 360px: retain one column and reduce only nonessential spacing.

All currently registered public routes use the dark-and-yellow shell; individual
page layouts remain purpose-built. Existing login, registration, and profile
components use the same visual system but their routes remain inactive pending a
separate account activation decision. The unregistered Ask component remains out
of scope. The canonical public identity is `https://morwalpiz.com`; runtime
configuration may override generated canonical URLs through
`VITE_CANONICAL_BASE_URL`.