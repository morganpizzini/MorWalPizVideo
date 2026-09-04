---
name: MorWalPizVideo Client
description: A creative-operational video hub with branded editorial content and practical production tools.
colors:
  brand-black: '#333333'
  brand-paper: '#ececec'
  surface-white: '#ffffff'
  surface-muted: '#f1f1f1'
  text-primary: '#1f1f1f'
  text-body: '#555555'
  text-muted: '#6c757d'
  bootstrap-primary: '#0d6efd'
  telegram-blue: '#2481cc'
  instagram-pink: '#e4405f'
  youtube-red: '#ff0000'
  quick-links-ink: '#18232d'
  quick-links-sage: '#d5e0dc'
  quick-links-terracotta: '#ab4b2c'
  quick-links-blush: '#f1d8ca'
typography:
  display:
    fontFamily: 'High Speed, sans-serif'
    fontSize: '64px'
    fontWeight: 700
    lineHeight: 1
    letterSpacing: '2px'
  headline:
    fontFamily: 'Georgia, Times New Roman, serif'
    fontSize: 'clamp(2rem, 7vw, 3.5rem)'
    fontWeight: 400
    lineHeight: 1.05
  body:
    fontFamily: 'Roboto, Arial, sans-serif'
    fontSize: '1rem'
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: 'Roboto, Arial, sans-serif'
    fontSize: '0.75rem'
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: '0.04em'
rounded:
  xs: '0.35rem'
  sm: '0.375rem'
  md: '0.5rem'
  lg: '0.65rem'
  card: '10px'
  circle: '50%'
spacing:
  xs: '0.35rem'
  sm: '0.6rem'
  md: '1rem'
  lg: '2rem'
  xl: '3rem'
components:
  button-primary:
    backgroundColor: '{colors.bootstrap-primary}'
    textColor: '{colors.surface-white}'
    rounded: '{rounded.sm}'
    padding: '0.375rem 0.75rem'
  button-social-telegram:
    backgroundColor: '{colors.telegram-blue}'
    textColor: '{colors.surface-white}'
    rounded: '5px'
    padding: '10px 20px'
  content-card:
    backgroundColor: '{colors.surface-white}'
    textColor: '{colors.text-primary}'
    rounded: '{rounded.card}'
    padding: '0.9rem 1rem 0.75rem'
  quick-link:
    backgroundColor: 'rgba(255, 255, 255, 0.78)'
    textColor: '{colors.quick-links-ink}'
    rounded: '{rounded.md}'
    padding: '1rem 1.1rem'
---

# Design System: MorWalPizVideo Client

## Overview

**Creative North Star: "Regia in movimento"**

MorWalPizVideo is a creative-operational video hub: the interface carries a visible creator identity while helping visitors scan content, reach social channels, manage tools, and follow live or scheduled activity. The main shell layers readable Bootstrap content over a photographic brand backdrop. Its signature wordmark uses the custom High Speed face, with oversized dark initials and pale supporting letters to create a compact, recognizable masthead.

The system is practical first, but it is not anonymous. Content cards, status alerts, filters, social actions, and forms use familiar Bootstrap patterns so repeated work stays predictable. The quick-links surface is a deliberate editorial variation: cool paper, sage, and terracotta create a calmer link directory while retaining the same direct, action-oriented rhythm.

**Key Characteristics:**

- Branded photographic atmosphere behind a readable content layer.
- Bootstrap utility language strengthened by custom creator branding.
- Content-led cards with image-first 16:9 media and compact metadata.
- Flat surfaces at rest, with restrained lift and shadow on interaction.

## Colors

The shared application palette is neutral and high-contrast, with saturated social colors reserved for recognizable channel actions. Quick links use a separate muted sage and terracotta register for an editorial directory experience.

### Primary

- **Bootstrap Action Blue** ({colors.bootstrap-primary}): Default primary actions, form submission, and operational calls to action.
- **Telegram Blue** ({colors.telegram-blue}): Telegram social action, kept distinct from generic application actions.

### Secondary

- **Instagram Pink** ({colors.instagram-pink}): Instagram social action.
- **YouTube Red** ({colors.youtube-red}): YouTube action and recognizable media affordance.
- **Quick-links Terracotta** ({colors.quick-links-terracotta}): Accent for directory headings, icon blocks, focus, and hover states.

### Tertiary

- **Quick-links Sage** ({colors.quick-links-sage}): Cool atmospheric endpoint for the quick-links background.
- **Quick-links Blush** ({colors.quick-links-blush}): Soft backing for quick-links icon tiles.

### Neutral

- **Brand Black** ({colors.brand-black}): Oversized wordmark initials and strong brand contrast.
- **Brand Paper** ({colors.brand-paper}): Supporting wordmark letters.
- **Surface White** ({colors.surface-white}): Cards, navigation bands, and readable content surfaces.
- **Muted Surface** ({colors.surface-muted}): Media placeholders and loading states.
- **Primary Ink** ({colors.text-primary}): Card titles and important content.
- **Body Gray** ({colors.text-body}): Supporting descriptions.
- **Muted Gray** ({colors.text-muted}): Metadata, categories, and secondary labels.
- **Quick-links Ink** ({colors.quick-links-ink}): Text color for the directory surface.

### Named Rules

**The Accent Rarity Rule.** Social colors and terracotta should identify a destination or state; they should not become a general page wash.

## Typography

**Display Font:** High Speed (with sans-serif fallback)
**Body Font:** Roboto (with Arial, sans-serif fallback)
**Label/Editorial Font:** Georgia (with Times New Roman, serif fallback for quick-links headlines)

**Character:** High Speed supplies the creator signature and a slightly kinetic masthead. Roboto keeps tools and dense content legible, while Georgia gives the quick-links directory a quieter editorial voice.

### Hierarchy

- **Display** (bold, 64px, line-height 1): Branded title header; oversized initials may reach 84px on desktop.
- **Headline** (regular, clamp(2rem, 7vw, 3.5rem), line-height 1.05): Quick-links page title.
- **Title** (600, 1.05rem, line-height 1.25): Content card titles and compact tool headings.
- **Body** (400, 1rem, line-height 1.5): Application copy and operational descriptions; keep longer text within readable Bootstrap containers.
- **Label** (700, 0.75rem, line-height 1.2, 0.04em): Uppercase categories, channel labels, and metadata.

### Named Rules

**The Two-Voice Rule.** Use High Speed only for the brand mark; use Roboto for the application and Georgia only for the quick-links editorial headline.

## Layout

The main application follows Bootstrap's responsive container and utility grid. Header branding is centered with 50px top padding, followed by social actions and dynamic navigation. Home content uses a single-column stack below 768px and a three-column card grid at 768px and above, with 1rem gaps. Cards preserve a 16:9 media ratio so content remains visually stable as the viewport changes.

Quick links use a centered column capped at 40rem, with 1.25rem horizontal page padding and 3rem vertical padding on larger screens. The directory becomes more compact at widths below 34rem. Filter controls wrap naturally; their tag group changes from a vertical divider to a full-width top divider below 576px.

## Elevation & Depth

This is a flat-with-state-lift system. The photographic body background and translucent white overlay create the base depth; cards and links are flat readable surfaces with ambient shadows. Interaction adds a small scale or upward translation and a stronger shadow, making the interface feel responsive without turning every element into a floating panel.

### Shadow Vocabulary

- **Global hover lift** (`0 4px 6px rgba(0, 0, 0, 0.4)`): Shared button and popup hover response.
- **Content card rest** (`0 4px 8px rgba(0, 0, 0, 0.2)`): Baseline home and match card separation.
- **Content card hover** (`0 8px 16px rgba(0, 0, 0, 0.4)`): Stronger emphasis during card interaction.
- **Quick-link ambient** (`0 0.75rem 2rem rgba(24, 35, 45, 0.08)`): Soft directory item separation.
- **Quick-link hover** (`0 1rem 2.25rem rgba(24, 35, 45, 0.14)`): Lifted focused or hovered directory item.

### Named Rules

**The State-Lift Rule.** Depth belongs to interaction and hierarchy: use shadow and translation to signal hover or focus, not as decoration on every surface.

## Shapes

The form language is gently rounded and familiar rather than geometric. Bootstrap controls use the framework's native small radius; custom home cards use 10px corners with clipped media, quick-links use 0.5rem corners, and icons use smaller 0.35rem tiles or circular avatars. Borders are sparse and functional, appearing mainly as dividers, input strokes, and quick-link focus boundaries.

## Components

### Buttons

- **Shape:** Small Bootstrap rounding for application actions; social buttons use 5px corners.
- **Primary:** Bootstrap action blue with white text and compact Bootstrap padding.
- **Hover / Focus:** Shared controls scale to 1.03 and gain a soft shadow; social buttons deepen to their channel-specific hover colors.
- **Secondary / Ghost / Tertiary:** Outline and muted Bootstrap variants support navigation, cancellation, filters, and status actions.

### Chips

- **Style:** Bootstrap badges use compact filled status colors such as secondary, danger, success, info, and primary.
- **State:** Category filters use small buttons; selected filters use filled info blue and unselected filters use outline info.

### Cards / Containers

- **Corner Style:** Home content cards use 10px corners; Bootstrap tool cards retain framework defaults.
- **Background:** White content surfaces over the translucent photographic shell; media placeholders use muted gray.
- **Shadow Strategy:** Resting content cards use a moderate ambient shadow and lift on hover.
- **Border:** Home cards are borderless; footer separators and utility dividers use light gray strokes.
- **Internal Padding:** Home card bodies use compact 0.9rem 1rem 0.75rem padding, with a slightly tighter footer.

### Inputs / Fields

- **Style:** Bootstrap `form-control` fields provide the shared input language and spacing.
- **Focus:** Use Bootstrap's primary focus treatment; preserve the standard control affordance.
- **Error / Disabled:** Bootstrap alert and validation states provide explicit feedback without changing the overall geometry.

### Navigation

- **Style:** Header navigation is centered and dynamic, with Bootstrap link utilities; footer navigation uses light links in a dark translucent footer. The stream surface uses a dark Bootstrap navbar.
- **States:** Social links are filled by destination color; standard links use Bootstrap hover and focus behavior.
- **Mobile:** Header title scales its large and small wordmark letters down at 768px; content navigation wraps rather than forcing horizontal overflow.

### Social Actions

The title header's Telegram, Instagram, and YouTube links are signature destination actions. Each uses a recognizable saturated channel color, white text, a compact rounded rectangle, and a small hover darkening response.

### Quick-link Directory Items

Quick links are the signature calm surface: each item is a translucent white row with a compact image or terracotta icon tile, title and truncated supporting text, plus a right-aligned terracotta arrow. Hover and focus-visible lift the row by 2px and strengthen its border and shadow.

## Do's and Don'ts

### Do:

- **Do** keep the High Speed typeface exclusive to the brand mark.
- **Do** preserve 16:9 media framing for home content cards.
- **Do** use saturated social colors only for the matching social destination.
- **Do** keep operational surfaces readable over the photographic background.
- **Do** let cards and quick links lift subtly on hover and focus-visible.
- **Do** retain the quick-links sage, paper, and terracotta atmosphere on that route.

### Don't:

- **Don't** replace the photographic backdrop with a flat application-wide fill without an explicit identity decision.
- **Don't** use High Speed for body copy, labels, or forms.
- **Don't** turn every surface into a heavily rounded floating card.
- **Don't** mix quick-links terracotta into unrelated Bootstrap status semantics.
- **Don't** let responsive card grids distort media proportions or create horizontal overflow.
