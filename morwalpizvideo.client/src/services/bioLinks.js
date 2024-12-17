export function getBioLinks() {
    return fetch(`/api/biolinks`)
        .then((response) => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        })
}