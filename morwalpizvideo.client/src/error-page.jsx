import { useRouteError } from "react-router";

export default function ErrorPage() {
    const error = useRouteError();
    console.error(error);

    return (
        <div className="container mt-3" id="error-page">
            <h1>Oops!</h1>
            <p>Sono spiacente, errore inatteso.</p>
            <p>
                <i>{error.statusText || error.message}</i>
            </p>
        </div>
    );
}