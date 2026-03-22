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

// Pages
const PAGES = `${baseEndpoint}/pages`;
const PAGES_DETAIL = `${PAGES}/{pageId}`;

// Products
const PRODUCTS = `${baseEndpoint}/products`;

// Sponsors
const SPONSORS = `${baseEndpoint}/sponsors`;

// Configuration
const CONFIGURATION_STREAM = `${baseEndpoint}/configuration/stream`;

// YouTube Video Links (Linktree)
const YOUTUBE_VIDEO_LINKS = `${baseEndpoint}/YouTubeVideoLinks`;
const YOUTUBE_VIDEO_LINKS_DETAIL = `${YOUTUBE_VIDEO_LINKS}/{matchId}/links`;
const YOUTUBE_VIDEO_LINKS_IMAGE = `${YOUTUBE_VIDEO_LINKS}/image/{imageName}`;

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
    PAGES,
    PAGES_DETAIL,
    PRODUCTS,
    SPONSORS,
    CONFIGURATION_STREAM,
    YOUTUBE_VIDEO_LINKS,
    YOUTUBE_VIDEO_LINKS_DETAIL,
    YOUTUBE_VIDEO_LINKS_IMAGE,
    APIKEYS,
    APIKEYS_DETAIL,
    APIKEYS_TOGGLE,
    APIKEYS_REGENERATE
};