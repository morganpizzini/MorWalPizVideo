import { useRouteError } from "react-router";
import TitleHeader from "@layouts/title-header";
import ErrorPage from "./error-page";
export default function ErrorPageRoot() {
    const error = useRouteError();
    console.error(error);

    return (
        <div>
            <TitleHeader />
            <ErrorPage/>
        </div>
    );
}