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
    CUSTOMFORMS,
    CUSTOMFORMS_DETAIL,
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
    IMAGE_UPLOAD_MULTIPLE
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
