import { getMatches } from "@services/matches";
import { getConfiguration } from "@services/stream";
import { getActiveForms } from "@services/customForms";

export default async function loader() {
    const responsePromise = getMatches(true);
    const configurationPromise = getConfiguration();
    const activeFormsPromise = getActiveForms();
    
    const [response, configuration, activeForms] = await Promise.all([
        responsePromise,
        configurationPromise,
        activeFormsPromise
    ]);
    console.log(response);
    return { 
        matches: response.data, 
        total: response.count, 
        next: response.next, 
        configuration: configuration,
        activeForms: activeForms || []
    };
}
