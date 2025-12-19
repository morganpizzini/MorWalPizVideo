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
    SPONSORS_DETAIL
}

export function ComposeUrl(inputString, replacements, queryStringObj = undefined) {
    if (!inputString) {
        console.error("Parameter inputString not provided in ComposeUrl function");
        return "";
    }
    // Regular expression pattern to match placeholders
    const pattern = /\{(.*?)\}/g;

    // Function to replace placeholders using a callback function
    function replacePlaceholder(match, placeholder) {
        return replacements[placeholder] || match;
    }
    let resultUrl = inputString.replace(pattern, replacePlaceholder);
    if (queryStringObj) {
        resultUrl = `${resultUrl}?${new URLSearchParams(queryStringObj).toString()}`;
    }
    // Use replace with the pattern and callback function to replace placeholders
    return resultUrl;
}
