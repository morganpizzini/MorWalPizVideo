import { Outlet, useNavigation } from "react-router";
import TitleComponent from "@layouts/title-header";
import Footer from "@layouts/footer";
import ScrollToTop from "@utils/scroll-to-top"; 
export default function Root() {
    const navigation = useNavigation();
    return (
        <>
            <ScrollToTop/>
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