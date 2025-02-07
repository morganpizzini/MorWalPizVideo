export function getSponsors() {
    return fetch('/api/sponsors')
        .then((response) => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        })
}

export function askForSponsor(name, email, description, token) {
    return fetch("/api/sponsors", {
        method: "POST",
        body: JSON.stringify({ name: name, email: email, description: description, token: token }),
        headers: {
            "Content-type": "application/json; charset=UTF-8"
        }
    }).then((response) => {
        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }
        return response.status == '204' ? true : response.json();
    }); 
}