/**
 * Frontend-specific API endpoints for public client application
 * These endpoints are used by morwalpizvideo.client (public-facing application)
 */

const baseEndpoint = "api";

// Calendar Events
const CALENDAREVENTS = `${baseEndpoint}/calendarEvents`;

// Matches
const MATCHES = `${baseEndpoint}/matches`;
const MATCHES_DETAIL = `${MATCHES}/{matchId}`;
const MATCHES_IMAGES = `${MATCHES}/{matchId}/images`;

// Channels (used by the FR-016 video↔channel join)
const CHANNELS = `${baseEndpoint}/channels`;
const CHANNEL_SOCIALS = "api/v1/channel/socials";
const SHIT_CHANNELS = `${baseEndpoint}/shit/channels`;
const SHIT_MATCHES = `${baseEndpoint}/shit/matches`;
const SHIT_QUICK_LINKS = `${baseEndpoint}/shit/quicklinks`;
const SHIT_QUICK_LINK_DETAIL = `${SHIT_QUICK_LINKS}/{customLinktree}`;
const SHIT_CHANNEL_NEWS = `${baseEndpoint}/shit/channelnews`;
const SHIT_CHANNEL_NEWS_DETAIL = `${SHIT_CHANNEL_NEWS}/{idOrSlug}`;

// Pages
const PAGES = `${baseEndpoint}/pages`;
const PAGES_DETAIL = `${PAGES}/{pageId}`;
const NAVIGATION = `${baseEndpoint}/navigation`;

// Products
const PRODUCTS = `${baseEndpoint}/products`;

// Sponsors
const SPONSORS = `${baseEndpoint}/sponsors`;
const NEWSLETTER_SUBSCRIBE = `${baseEndpoint}/newsletter/subscribe`;
const NEWSLETTER_CONFIRM = `${baseEndpoint}/newsletter/confirm`;
const NEWSLETTER_UNSUBSCRIBE = `${baseEndpoint}/newsletter/unsubscribe`;

// Web Push (anonymous, per-channel)
const PUSH_PUBLIC_KEY = `${baseEndpoint}/push/subscriptions/public-key`;
const PUSH_SUBSCRIPTIONS = `${baseEndpoint}/push/subscriptions`;
const PUSH_SUBSCRIPTION_SETTINGS = `${PUSH_SUBSCRIPTIONS}/settings`;
const PUSH_SUBSCRIPTION_REVOKE = `${PUSH_SUBSCRIPTIONS}/revoke`;

// Public account (cookie + CSRF isolated from public/admin transports)
const PUBLIC_ACCOUNT_REGISTER = "api/v1/accounts";
const PUBLIC_ACCOUNT_LOGIN = "api/v1/account/sessions";
const PUBLIC_ACCOUNT_SESSION = "api/v1/account/session";
const PUBLIC_ACCOUNT_LOGOUT = "api/v1/account/sessions/current";
const PUBLIC_ACCOUNT_CSRF = "api/v1/account/csrf";
const PUBLIC_ACCOUNT_PROFILE = "api/v1/profile";
const PUBLIC_FEEDBACK = "api/v1/feedback";
const PUBLIC_BLOG_INTERACTIONS = `${baseEndpoint}/blog/{slug}/interactions`;

// Account-owned Shooter Workshop API. Keep these paths isolated from the
// public/admin transports; workshopService uses accountApiService exclusively.
const WORKSHOP = "api/v1/workshop";
const WORKSHOP_FIREARMS = `${WORKSHOP}/firearms`;
const WORKSHOP_FIREARM_DETAIL = `${WORKSHOP_FIREARMS}/{firearmId}`;
const WORKSHOP_FIREARM_DETAIL_READ = `${WORKSHOP_FIREARM_DETAIL}/detail`;
const WORKSHOP_COMPONENT_DEFINITIONS = `${WORKSHOP}/components/definitions`;
const WORKSHOP_COMPONENT_DEFINITION_DETAIL = `${WORKSHOP_COMPONENT_DEFINITIONS}/{definitionId}`;
const WORKSHOP_COMPONENTS = `${WORKSHOP}/components`;
const WORKSHOP_COMPONENT_DETAIL = `${WORKSHOP_COMPONENTS}/{componentId}`;
const WORKSHOP_COMPONENT_INSTALLATIONS = `${WORKSHOP_COMPONENTS}/installations`;
const WORKSHOP_COMPONENT_INSTALL = `${WORKSHOP_COMPONENTS}/install`;
const WORKSHOP_MAINTENANCE = `${WORKSHOP}/maintenance`;
const WORKSHOP_RECIPES = `${WORKSHOP}/recipes`;
const WORKSHOP_RECIPE_DETAIL = `${WORKSHOP_RECIPES}/{recipeId}`;
const WORKSHOP_RECIPE_JOURNAL = `${WORKSHOP_RECIPE_DETAIL}/journal`;
const WORKSHOP_AMMUNITION = `${WORKSHOP}/ammunition`;
const WORKSHOP_AMMUNITION_PRODUCTION = `${WORKSHOP_AMMUNITION}/production`;
const WORKSHOP_AMMUNITION_PRODUCTION_DETAIL = `${WORKSHOP_AMMUNITION_PRODUCTION}/{sessionId}`;
const WORKSHOP_SHOOTING = `${WORKSHOP}/sessions`;
const WORKSHOP_SHOOTING_READ = `${WORKSHOP_SHOOTING}/shooting`;
const WORKSHOP_SHOOTING_CORRECT = `${WORKSHOP_SHOOTING}/shooting/correct`;
const WORKSHOP_DASHBOARD = `${WORKSHOP}/dashboard`;
const WORKSHOP_ANALYTICS = `${WORKSHOP}/analytics`;
const WORKSHOP_HISTORY = `${WORKSHOP}/history`;
const WORKSHOP_REPORT = `${WORKSHOP}/report`;
const WORKSHOP_RECONCILIATION = `${WORKSHOP}/reconciliation`;
const WORKSHOP_CAPABILITY_GUIDE_SEEN = `${WORKSHOP}/capability-guide/seen`;
const WORKSHOP_INVENTORY = `${WORKSHOP}/inventory`;
const WORKSHOP_INVENTORY_PRODUCTS = `${WORKSHOP_INVENTORY}/products`;
const WORKSHOP_INVENTORY_PURCHASES = `${WORKSHOP_INVENTORY}/purchases`;
const WORKSHOP_INVENTORY_ADJUSTMENTS = `${WORKSHOP_INVENTORY}/adjustments`;
const WORKSHOP_INVENTORY_LOT_STATUS = `${WORKSHOP_INVENTORY}/lots/{lotId}/status`;
const WORKSHOP_COMPONENT_PURCHASE = `${WORKSHOP_INVENTORY}/components/{componentId}/purchase`;

// Configuration
const CONFIGURATION_STREAM = `${baseEndpoint}/configuration/stream`;

// Public QuickLinks
const QUICK_LINKS = `${baseEndpoint}/quicklinks`;
const QUICK_LINKS_DETAIL = `${QUICK_LINKS}/{url}`;
const GALLERIES = `${baseEndpoint}/galleries`;
const GALLERY_DETAIL = `${GALLERIES}/{slug}`;
const GALLERY_DOWNLOAD = `${GALLERY_DETAIL}/download`;

// API Keys (for public client admin features)
const APIKEYS = `${baseEndpoint}/apikeys`;
const APIKEYS_DETAIL = `${APIKEYS}/{id}`;
const APIKEYS_TOGGLE = `${APIKEYS}/{id}/toggle`;
const APIKEYS_REGENERATE = `${APIKEYS}/{id}/regenerate`;

export default {
  CALENDAREVENTS,
  MATCHES,
  MATCHES_DETAIL,
  MATCHES_IMAGES,
  CHANNELS,
  CHANNEL_SOCIALS,
  SHIT_CHANNELS,
  SHIT_MATCHES,
  SHIT_QUICK_LINKS,
  SHIT_QUICK_LINK_DETAIL,
  SHIT_CHANNEL_NEWS,
  SHIT_CHANNEL_NEWS_DETAIL,
  PAGES,
  PAGES_DETAIL,
  NAVIGATION,
  PRODUCTS,
  SPONSORS,
  NEWSLETTER_SUBSCRIBE,
  NEWSLETTER_CONFIRM,
  NEWSLETTER_UNSUBSCRIBE,
  PUSH_PUBLIC_KEY,
  PUSH_SUBSCRIPTIONS,
  PUSH_SUBSCRIPTION_SETTINGS,
  PUSH_SUBSCRIPTION_REVOKE,
  PUBLIC_ACCOUNT_REGISTER,
  PUBLIC_ACCOUNT_LOGIN,
  PUBLIC_ACCOUNT_SESSION,
  PUBLIC_ACCOUNT_LOGOUT,
  PUBLIC_ACCOUNT_CSRF,
  PUBLIC_ACCOUNT_PROFILE,
  PUBLIC_FEEDBACK,
  PUBLIC_BLOG_INTERACTIONS,
  WORKSHOP,
  WORKSHOP_FIREARMS,
  WORKSHOP_FIREARM_DETAIL,
  WORKSHOP_FIREARM_DETAIL_READ,
  WORKSHOP_COMPONENT_DEFINITIONS,
  WORKSHOP_COMPONENT_DEFINITION_DETAIL,
  WORKSHOP_COMPONENTS,
  WORKSHOP_COMPONENT_DETAIL,
  WORKSHOP_COMPONENT_INSTALLATIONS,
  WORKSHOP_COMPONENT_INSTALL,
  WORKSHOP_MAINTENANCE,
  WORKSHOP_RECIPES,
  WORKSHOP_RECIPE_DETAIL,
  WORKSHOP_RECIPE_JOURNAL,
  WORKSHOP_AMMUNITION,
  WORKSHOP_AMMUNITION_PRODUCTION,
  WORKSHOP_AMMUNITION_PRODUCTION_DETAIL,
  WORKSHOP_SHOOTING,
  WORKSHOP_SHOOTING_READ,
  WORKSHOP_SHOOTING_CORRECT,
  WORKSHOP_DASHBOARD,
  WORKSHOP_ANALYTICS,
  WORKSHOP_HISTORY,
  WORKSHOP_REPORT,
  WORKSHOP_RECONCILIATION,
  WORKSHOP_CAPABILITY_GUIDE_SEEN,
  WORKSHOP_INVENTORY,
  WORKSHOP_INVENTORY_PRODUCTS,
  WORKSHOP_INVENTORY_PURCHASES,
  WORKSHOP_INVENTORY_ADJUSTMENTS,
  WORKSHOP_INVENTORY_LOT_STATUS,
  WORKSHOP_COMPONENT_PURCHASE,
  CONFIGURATION_STREAM,
  QUICK_LINKS,
  QUICK_LINKS_DETAIL,
  GALLERIES,
  GALLERY_DETAIL,
  GALLERY_DOWNLOAD,
  APIKEYS,
  APIKEYS_DETAIL,
  APIKEYS_TOGGLE,
  APIKEYS_REGENERATE,
};
