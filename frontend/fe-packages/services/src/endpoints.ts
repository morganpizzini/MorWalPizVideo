const baseEndpoint = 'api';

const VIDEOS = `${baseEndpoint}/videos`;
const VIDEOS_DETAIL = `${VIDEOS}/{videoId}`;
const CATEGORIES = `${baseEndpoint}/categories`;
const CATEGORIES_DETAIL = `${CATEGORIES}/{categoryId}`;
const QUERYLINKS = `${baseEndpoint}/querylinks`;
const QUERYLINKS_DETAIL = `${QUERYLINKS}/{querylinkId}`;
const SHORTLINKS = `${baseEndpoint}/shortlinks`;
const SHORTLINKS_DETAIL = `${SHORTLINKS}/{querylinkId}`;
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
const CHANNELS_DETAIL = `${CHANNELS}/{channelId}`;
const CONFIGURATIONS = `${baseEndpoint}/configurations`;
const CONFIGURATIONS_DETAIL = `${CONFIGURATIONS}/{configurationId}`;
const VIDEOS_IMPORT = `${VIDEOS}/ImportVideo`;
const VIDEOS_IMPORT_SUB = `${VIDEOS}/ImportSubCreation`;
const VIDEOS_ROOT_CREATION = `${VIDEOS}/RootCreation`;
const VIDEOS_CONVERT_TO_ROOT = `${VIDEOS}/ConvertIntoRoot`;
const VIDEOS_SWAP_THUMBNAIL = `${VIDEOS}/SwapThumbnailId`;
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
const SHOP_CART_ITEMS = `${SHOP_CART}/items`;
const SHOP_CART_CHECKOUT = `${SHOP_CART}/checkout`;
const SHOP_LEGAL = `${baseEndpoint}/shop/legal/{type}`;

// API Keys endpoints
const APIKEYS = `${baseEndpoint}/apikeys`;
const APIKEYS_DETAIL = `${APIKEYS}/{id}`;
const APIKEYS_TOGGLE = `${APIKEYS}/{id}/toggle`;
const APIKEYS_REGENERATE = `${APIKEYS}/{id}/regenerate`;

export default {

    VIDEOS,
    VIDEOS_DETAIL,
    CATEGORIES,
    CATEGORIES_DETAIL,
    QUERYLINKS,
    QUERYLINKS_DETAIL,
    SHORTLINKS,
    SHORTLINKS_DETAIL,
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
    CHANNELS_DETAIL,
    CONFIGURATIONS,
    CONFIGURATIONS_DETAIL,
    VIDEOS_IMPORT,
    VIDEOS_IMPORT_SUB,
    VIDEOS_ROOT_CREATION,
    VIDEOS_CONVERT_TO_ROOT,
    VIDEOS_SWAP_THUMBNAIL,
    VIDEOS_TRANSLATE,
    IMAGE_UPLOAD,
    IMAGE_UPLOAD_MULTIPLE,
    SHOP_PRODUCTS,
    SHOP_PRODUCTS_DETAIL,
    SHOP_PRODUCT_CATEGORIES,
    SHOP_AUTH_LOGIN,
    SHOP_AUTH_VERIFY,
    SHOP_CART,
    SHOP_CART_ITEMS,
    SHOP_CART_CHECKOUT,
    SHOP_LEGAL,
    APIKEYS,
    APIKEYS_DETAIL,
    APIKEYS_TOGGLE,
    APIKEYS_REGENERATE,
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