import { get, post, frontendEndpoints } from '@morwalpizvideo/services';

export function getSponsors() {
    return get(frontendEndpoints.SPONSORS);
}

export function askForSponsor(name: string, email: string, description: string, token: string) {
    return post(frontendEndpoints.SPONSORS, { 
        name, 
        email, 
        description, 
        token 
    }).then((response) => {
        // Handle 204 No Content response
        if (!response || Object.keys(response).length === 0) {
            return true;
        }
        return response;
    });
}