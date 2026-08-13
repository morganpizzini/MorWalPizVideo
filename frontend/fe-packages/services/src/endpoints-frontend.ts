/**
 * Frontend-specific API endpoints for public client application
 * These endpoints are used by morwalpizvideo.client (public-facing application)
 */

const baseEndpoint = 'api';

// Bio Links
const BIOLINKS = `${baseEndpoint}/biolinks`;

// Calendar Events
const CALENDAREVENTS = `${baseEndpoint}/calendarEvents`;

// Matches
const MATCHES = `${baseEndpoint}/matches`;
const MATCHES_DETAIL = `${MATCHES}/{matchId}`;
const MATCHES_IMAGES = `${MATCHES}/{matchId}/images`;

// Channels (used by the FR-016 video↔channel join)
const CHANNELS = `${baseEndpoint}/channels`;

// Pages
const PAGES = `${baseEndpoint}/pages`;
const PAGES_DETAIL = `${PAGES}/{pageId}`;

// Products
const PRODUCTS = `${baseEndpoint}/products`;

// Sponsors
const SPONSORS = `${baseEndpoint}/sponsors`;

// Configuration
const CONFIGURATION_STREAM = `${baseEndpoint}/configuration/stream`;

// Public QuickLinks
const QUICK_LINKS = `${baseEndpoint}/quicklinks`;
const QUICK_LINKS_DETAIL = `${QUICK_LINKS}/{url}`;

// API Keys (for public client admin features)
const APIKEYS = `${baseEndpoint}/apikeys`;
const APIKEYS_DETAIL = `${APIKEYS}/{id}`;
const APIKEYS_TOGGLE = `${APIKEYS}/{id}/toggle`;
const APIKEYS_REGENERATE = `${APIKEYS}/{id}/regenerate`;

export default {
    BIOLINKS,
    CALENDAREVENTS,
    MATCHES,
    MATCHES_DETAIL,
    MATCHES_IMAGES,
    CHANNELS,
    PAGES,
    PAGES_DETAIL,
    PRODUCTS,
    SPONSORS,
    CONFIGURATION_STREAM,
    QUICK_LINKS,
    QUICK_LINKS_DETAIL,
    APIKEYS,
    APIKEYS_DETAIL,
    APIKEYS_TOGGLE,
    APIKEYS_REGENERATE
};