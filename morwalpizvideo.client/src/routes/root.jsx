import { Outlet, useNavigation } from "react-router";
import TitleComponent from "@layouts/title-header";
import Footer from "@layouts/footer";

export default function Root() {
    const navigation = useNavigation();
    return (
        <>
            <TitleComponent />
            <div className={
                `container my-5 ${navigation.state === "loading" ? "loading" : ""}`
            }>
                <Outlet />
            </div>
            <Footer/>
        </>
    );
}