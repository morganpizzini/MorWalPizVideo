const API_BASE_URL = '/api';
export const VideoService = {
    translateShort: async (videoIds) => {
        const response = await fetch(`${API_BASE_URL}/Video/Translate`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(videoIds),
        });
        if (!response.ok) {
            throw new Error(`Error translating videos: ${response.statusText}`);
        }
    },
    importVideo: async (request) => {
        const response = await fetch(`${API_BASE_URL}/Video/ImportVideo`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(request),
        });
        if (!response.ok) {
            throw new Error(`Error importing video: ${response.statusText}`);
        }
    },
    convertToRoot: async (request) => {
        const response = await fetch(`${API_BASE_URL}/Video/ConvertIntoRoot`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(request),
        });
        if (!response.ok) {
            throw new Error(`Error converting video to root: ${response.statusText}`);
        }
    },
    swapThumbnailUrl: async (request) => {
        const response = await fetch(`${API_BASE_URL}/Video/SwapThumbnailId`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(request),
        });
        if (!response.ok) {
            throw new Error(`Error swapping thumbnail: ${response.statusText}`);
        }
    },
    createRoot: async (request) => {
        const response = await fetch(`${API_BASE_URL}/Video/RootCreation`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(request),
        });
        if (!response.ok) {
            throw new Error(`Error creating root video: ${response.statusText}`);
        }
    },
    createSubVideo: async (request) => {
        const response = await fetch(`${API_BASE_URL}/Video/ImportSubCreation`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(request),
        });
        if (!response.ok) {
            throw new Error(`Error creating sub-video: ${response.statusText}`);
        }
    },
    getReviewDetails: async (reviewText) => {
        const response = await fetch(`${API_BASE_URL}/Chat`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(reviewText),
        });
        if (!response.ok) {
            throw new Error(`Error getting review details: ${response.statusText}`);
        }
        return response.json();
    },
};
