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
rounded:
  sm: '4px'
  md: '8px'
  lg: '10px'
  circle: '50%'
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

The public homepage is a dark editorial hub for Morgan Pizzini's videos, series,
articles, gear, sponsors, and community. It is designed for quick scanning first,
with strong media, concise metadata, and a yellow signal accent. The homepage uses
this system while legacy routes retain their existing light presentation.

## Colors

- **Magazine black** (`#101010`) is the page background and header/footer base.
- **Magazine surface** (`#181818`) frames panels and cards.
- **Magazine elevated** (`#222222`) supports alerts, media placeholders, and menus.
- **Signal yellow** (`#ffff36`) identifies primary actions, selected filters, and active navigation.
- **White** (`#ffffff`) is reserved for primary content and link text.
- **Secondary gray** (`#b8b8b8`) is used for supporting text and metadata.
- **Border gray** (`#383838`) separates dense editorial regions.
- Social colors identify their destination only: Telegram, Instagram, YouTube, and Discord.

The palette is intentionally high contrast. Yellow is an action and state color,
not a page wash.

## Typography

Manrope is used throughout the public shell. The brand wordmark remains the image
asset in the header. Headings use strong weight and compact line height; supporting
copy stays restrained and wraps naturally rather than clipping.

## Layout

The homepage is capped at 1540px with 30px desktop gutters and 16px phone gutters.
The featured region combines a wide content panel with Follow and Donation panels.
The latest feed is a four-column desktop grid, three columns on tablet, two columns
on larger phones, and one column below 480px so card titles and metadata remain
readable. Article cards follow the same responsive rule.

Media keeps stable 16:9 boxes. Filters scroll horizontally within their own row;
the page itself must never acquire horizontal overflow. Safe-area insets are
included in the sticky header and footer for full-screen mobile browsers.

## Interaction

The header becomes a disclosure menu below 1200px. It exposes expanded state,
focuses the first menu item when opened, traps Tab within the open navigation,
closes on Escape or route activation, and returns focus to the toggle. Search,
account, social, filter, share, and editorial controls provide visible focus and
44px coarse-pointer targets. Hover-only media transforms run only for fine pointers.

Reduced-motion preferences remove authored movement while preserving immediate color,
focus, and selected-state feedback.

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

### Header and footer

The sticky header provides the brand, route navigation, search, social destinations,
and authenticated profile action. The footer keeps configured navigation plus stable
Sponsor, Gear, Articles, Donation, Privacy, Terms, and consent destinations.

## Responsive rules

- 1200px: collapse primary navigation into a menu and stack sponsor/gear regions.
- 768px: stack the feature region and reduce page gutters.
- 480px: use one content column, stack Follow and Donation, and preserve full-width controls.
- 360px: retain one column and reduce only nonessential spacing.

All public routes outside the homepage preserve their existing light shell. The
canonical public identity is `https://morwalpiz.com`; runtime configuration may
override generated canonical URLs through `VITE_CANONICAL_BASE_URL`.