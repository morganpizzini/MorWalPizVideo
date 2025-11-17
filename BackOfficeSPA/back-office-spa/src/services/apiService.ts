export function post(url, obj, overrideHeaderEnv: string = '') {
    return call(url, 'POST', obj, overrideHeaderEnv);
}

export function postFormData(url, formData: FormData, overrideHeaderEnv: string = '') {
    return call(url, 'POST', formData, overrideHeaderEnv, undefined, false, true);
}
export function put(url, obj, overrideHeaderEnv: string = '') {
    return call(url, 'PUT', obj, overrideHeaderEnv);
}
export function patch(url, obj, overrideHeaderEnv: string = '') {
    return call(url, 'PATCH', obj, overrideHeaderEnv);
}
export function get(url: string, query?, overrideHeaderEnv: string = '') {
    return call(url, 'GET', {}, overrideHeaderEnv, query);
}
export function getFile(url, query?, overrideHeaderEnv: string = '') {
    return call(url, 'GET', {}, overrideHeaderEnv, query, true);
}
export function Delete(url, query?, overrideHeaderEnv: string = '') {
    return call(url, 'DELETE', {}, overrideHeaderEnv, query);
}
/**
 * Attaches a given access token to a Microsoft Graph API call. Returns information about the user
 */
export async function call(url, method, body, overrideHeaderEnv: string = '', query?, downloadFile = false, isFormData = false) {
    const headers = new Headers();

    // Don't set Content-Type for FormData - let the browser set it with the boundary
    if (!isFormData) {
        headers.append("Content-Type", 'application/json');
    }

    if (query) {
        url += `?${new URLSearchParams(query).toString()}`;
    }

    const options: RequestInit = {
        method: method,
        headers: headers //,
        //credentials: "same-origin" as RequestCredentials
    };

    if (body) {
        if (isFormData) {
            // For FormData, use it directly
            options.body = body as FormData;
        } else if (Object.keys(body).length > 0) {
            // For regular objects, stringify them
            options.body = JSON.stringify(body);
        }
    }
    return fetch(`/${url}`, options)
        .then(async (response) => {
            if (response.ok) {
                if (downloadFile) {
                    return response.blob();
                }
                if (response.status === 204) {
                    return Promise.resolve({});
                }
                const parsedResponse = await response.json();
                return Object.prototype.hasOwnProperty.call(parsedResponse, 'data')
                    ? parsedResponse.data
                    : parsedResponse;
            } else {
                // error handling
                const errorMessages = [];
                switch (response.status) {
                    case 400:
                        {
                            const parsedResponse = await response.json();
                            if (typeof parsedResponse === 'object') {
                                for (const key in parsedResponse) {
                                    if (Array.isArray(parsedResponse[key])) {
                                        errorMessages.push(...parsedResponse[key]);
                                    } else {
                                        errorMessages.push(parsedResponse[key]);
                                    }
                                }
                            } else {
                                errorMessages.push(parsedResponse);
                            }
                            break;
                        }
                    case 404:
                        errorMessages.push({ api: "Not found" });
                        break;
                    default:
                        errorMessages.push("An unexpected error occurred");
                }
                return {
                    errors: errorMessages
                };
            }
        })
        .catch(error => { console.log("api error", error) });

}
