import { Outlet, useNavigation } from "react-router-dom";
import TitleComponent from "@layouts/title-header";

export default function Root() {
    const navigation = useNavigation();
    return (
        <>
            <TitleComponent />
            <div className={
                `container mt-5 ${navigation.state === "loading" ? "loading" : ""}`
            }>
                <Outlet />
            </div>
        </>
    );
}