const baseEndpoint = 'api';

const VIDEOS = `${baseEndpoint}/videos`;
const VIDEOS_DETAIL = `${VIDEOS}/{videoId}`;
const CATEGORIES = `${baseEndpoint}/categories`;
const CATEGORIES_DETAIL = `${CATEGORIES}/{categoryId}`;
const QUERYLINKS = `${baseEndpoint}/querylinks`;
const QUERYLINKS_DETAIL = `${QUERYLINKS}/{querylinkId}`;
const SHORTLINKS = `${baseEndpoint}/shortlinks`;
const SHORTLINKS_DETAIL = `${SHORTLINKS}/{querylinkId}`;
const QUICKLINKS = `${baseEndpoint}/quicklinks`;
const QUICKLINKS_DETAIL = `${QUICKLINKS}/{quickLinksId}`;
const PRODUCTS = `${baseEndpoint}/products`;
const PRODUCTS_DETAIL = `${PRODUCTS}/{productId}`;
const PRODUCTCATEGORIES = `${baseEndpoint}/productcategories`;
const PRODUCTCATEGORIES_DETAIL = `${PRODUCTCATEGORIES}/{productCategoryId}`;
const SPONSORS = `${baseEndpoint}/sponsors`;
const SPONSORS_DETAIL = `${SPONSORS}/{sponsorId}`;
const COMPILATIONS = `${baseEndpoint}/compilations`;
const COMPILATIONS_DETAIL = `${COMPILATIONS}/{compilationId}`;
const CUSTOMFORMS = `${baseEndpoint}/customforms`;
const CUSTOMFORMS_DETAIL = `${CUSTOMFORMS}/{customFormId}`;
const CUSTOMFORMS_ACTIVE = `${CUSTOMFORMS}/active`;
const CUSTOMFORMS_BY_URL = `${CUSTOMFORMS}/url/{url}`;
const CUSTOMFORMS_RESPONSES = `${CUSTOMFORMS}/{customFormId}/responses`;
const COMPILATIONS_BY_URL = `${baseEndpoint}/compilations/{url}`;
const CALENDAREVENTS = `${baseEndpoint}/calendarEvents`;
const CALENDAREVENTS_DETAIL = `${CALENDAREVENTS}/{title}`;
const CHANNELS = `${baseEndpoint}/channels`;
const CHANNELS_ACCESSIBLE = `${CHANNELS}/accessible`;
const CHANNELS_DETAIL = `${CHANNELS}/{channelId}`;
const CHANNEL_LOGO = `${CHANNELS_DETAIL}/logo`;
const CHANNEL_NEWS = `${baseEndpoint}/channelnews`;
const CHANNEL_NEWS_DETAIL = `${CHANNEL_NEWS}/{id}`;
const CHANNEL_NEWS_STATUS = `${CHANNEL_NEWS_DETAIL}/status`;
const CHANNEL_NEWS_IMAGES = `${CHANNEL_NEWS_DETAIL}/images`;
const CHANNEL_NEWS_IMAGE_DETAIL = `${CHANNEL_NEWS_IMAGES}/{imageIndex}`;
const CONFIGURATIONS = `${baseEndpoint}/configurations`;
const CONFIGURATIONS_DETAIL = `${CONFIGURATIONS}/{configurationId}`;
const VIDEOS_IMPORT = `${VIDEOS}/ImportVideo`;
const VIDEOS_TRANSLATE = `${VIDEOS}/translate`;
const IMAGE_UPLOAD = `${baseEndpoint}/ImageUpload/upload`;
const IMAGE_UPLOAD_MULTIPLE = `${baseEndpoint}/ImageUpload/upload-multiple`;

// Shop endpoints
const SHOP_PRODUCTS = `${baseEndpoint}/shop/products`;
const SHOP_PRODUCTS_DETAIL = `${SHOP_PRODUCTS}/{productId}`;
const SHOP_PRODUCT_CATEGORIES = `${baseEndpoint}/shop/categories`;
const SHOP_AUTH_LOGIN = `${baseEndpoint}/shop/auth/login`;
const SHOP_AUTH_VERIFY = `${baseEndpoint}/shop/auth/verify`;
const SHOP_CART = `${baseEndpoint}/shop/cart`;
const SHOP_CART_DETAIL = `${SHOP_CART}/{customerId}`;
const SHOP_CART_ITEMS = `${SHOP_CART_DETAIL}/items`;
const SHOP_CART_ITEM_DETAIL = `${SHOP_CART_ITEMS}/{productId}`;
const SHOP_CART_CHECKOUT = `${SHOP_CART_DETAIL}/checkout`;
const SHOP_LEGAL = `${baseEndpoint}/shop/legal/{type}`;

// API Keys endpoints
const APIKEYS = `${baseEndpoint}/apikeys`;
const APIKEYS_DETAIL = `${APIKEYS}/{id}`;
const APIKEYS_TOGGLE = `${APIKEYS}/{id}/toggle`;
const APIKEYS_REGENERATE = `${APIKEYS}/{id}/regenerate`;

// BackOffice user profile endpoints
const USERS = `${baseEndpoint}/user`;
const USER_DETAIL = `${USERS}/{id}`;
const USER_STATUS = `${USER_DETAIL}/status`;
const USER_PASSWORD_RESET = `${USER_DETAIL}/password/reset`;
const USER_PASSWORD_SET = `${USER_DETAIL}/password/set`;
const USER_ME = `${baseEndpoint}/user/me`;
const USER_ME_PASSWORD = `${USER_ME}/password`;

// RBAC endpoints
const RBAC = `${baseEndpoint}/rbac`;
const RBAC_USERS = `${RBAC}/users`;
const RBAC_USER_DETAIL = `${RBAC_USERS}/{id}`;
const RBAC_USER_PERMISSIONS = `${RBAC}/users/{id}/permissions`;
const RBAC_USER_GROUPS = `${RBAC}/users/{id}/groups`;
const RBAC_USER_GROUP = `${RBAC}/users/{id}/groups/{groupId}`;
const RBAC_USER_CHANNELS = `${RBAC}/users/{id}/channels`;
const RBAC_GROUPS = `${RBAC}/groups`;
const RBAC_GROUPS_DETAIL = `${RBAC_GROUPS}/{id}`;
const RBAC_GROUP_PERMISSIONS = `${RBAC_GROUPS}/{id}/permissions`;
const DASHBOARD_SUMMARY = `${baseEndpoint}/dashboard/summary`;
const DASHBOARD_VIDEO_PUBLICATIONS = `${baseEndpoint}/dashboard/video-publications`;

export default {

    VIDEOS,
    VIDEOS_DETAIL,
    CATEGORIES,
    CATEGORIES_DETAIL,
    QUERYLINKS,
    QUERYLINKS_DETAIL,
    SHORTLINKS,
    SHORTLINKS_DETAIL,
    QUICKLINKS,
    QUICKLINKS_DETAIL,
    PRODUCTS,
    PRODUCTS_DETAIL,
    PRODUCTCATEGORIES,
    PRODUCTCATEGORIES_DETAIL,
    SPONSORS,
    SPONSORS_DETAIL,
    COMPILATIONS,
    COMPILATIONS_DETAIL,
    COMPILATIONS_BY_URL,
    CUSTOMFORMS,
    CUSTOMFORMS_DETAIL,
    CUSTOMFORMS_ACTIVE,
    CUSTOMFORMS_BY_URL,
    CUSTOMFORMS_RESPONSES,
    CALENDAREVENTS,
    CALENDAREVENTS_DETAIL,
    CHANNELS,
    CHANNELS_ACCESSIBLE,
    CHANNELS_DETAIL,
    CHANNEL_LOGO,
    CHANNEL_NEWS,
    CHANNEL_NEWS_DETAIL,
    CHANNEL_NEWS_STATUS,
    CHANNEL_NEWS_IMAGES,
    CHANNEL_NEWS_IMAGE_DETAIL,
    CONFIGURATIONS,
    CONFIGURATIONS_DETAIL,
    VIDEOS_IMPORT,
    VIDEOS_TRANSLATE,
    IMAGE_UPLOAD,
    IMAGE_UPLOAD_MULTIPLE,
    SHOP_PRODUCTS,
    SHOP_PRODUCTS_DETAIL,
    SHOP_PRODUCT_CATEGORIES,
    SHOP_AUTH_LOGIN,
    SHOP_AUTH_VERIFY,
    SHOP_CART,
    SHOP_CART_DETAIL,
    SHOP_CART_ITEMS,
    SHOP_CART_ITEM_DETAIL,
    SHOP_CART_CHECKOUT,
    SHOP_LEGAL,
    APIKEYS,
    APIKEYS_DETAIL,
    APIKEYS_TOGGLE,
    APIKEYS_REGENERATE,
    USERS,
    USER_DETAIL,
    USER_STATUS,
    USER_PASSWORD_RESET,
    USER_PASSWORD_SET,
    USER_ME,
    USER_ME_PASSWORD,
    RBAC,
    RBAC_USERS,
    RBAC_USER_DETAIL,
    RBAC_USER_PERMISSIONS,
    RBAC_USER_GROUPS,
    RBAC_USER_GROUP,
    RBAC_USER_CHANNELS,
    RBAC_GROUPS,
    RBAC_GROUPS_DETAIL,
    RBAC_GROUP_PERMISSIONS,
    DASHBOARD_SUMMARY,
    DASHBOARD_VIDEO_PUBLICATIONS,
}

export function ComposeUrl(inputString: string, replacements: Record<string, string>, queryStringObj: Record<string, string> | undefined = undefined): string {
    if (!inputString) {
        console.error("Parameter inputString not provided in ComposeUrl function");
        return "";
    }
    // Regular expression pattern to match placeholders
    const pattern = /\{(.*?)\}/g;

    // Function to replace placeholders using a callback function
    function replacePlaceholder(match: string, placeholder: string): string {
        return replacements[placeholder] || match;
    }
    let resultUrl = inputString.replace(pattern, replacePlaceholder);
    if (queryStringObj) {
        resultUrl = `${resultUrl}?${new URLSearchParams(queryStringObj).toString()}`;
    }
    // Use replace with the pattern and callback function to replace placeholders
    return resultUrl;
}