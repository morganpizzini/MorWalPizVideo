export function getCompilationByUrl(url) {
    return fetch(`/api/compilations/${url}`)
        .then((response) => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        })
}
