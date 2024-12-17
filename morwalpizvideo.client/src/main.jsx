import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './main.scss'
import Root from "./routes/root";
import ErrorPage from "./error-page";
import Bio from "./routes/bio"
import Matches from "./routes/matches";
import Pages from "./routes/pages";
import Accessories from "./routes/accessories"
import Sponsors from "./routes/sponsors"
import Calendar from "./routes/calendar"
import Index from "./routes/index";
import bioLoader from "./routes/bio.loader"
import matchLoader from "./routes/matches.loader";
import pageLoader from "./routes/pages.loader";
import accessoryLoader from "./routes/accessories.loader";
import sponsorsLoader from "./routes/sponsors.loader";
import sponsorsAction from "./routes/sponsors.action";
import calendarLoader from "./routes/calendar.loader";
import indexLoader from "./routes/index.loader";
import CookiePolicy from "./routes/cookie-policy";
import {
    createBrowserRouter,
} from "react-router";
import { RouterProvider } from "react-router/dom";
import { HelmetProvider } from 'react-helmet-async';
import ReactGA from 'react-ga4';
import BuyMeWidget from "@utils/buy-me-widget";

import {
    GoogleReCaptchaProvider
} from 'react-google-recaptcha-v3';

const router = createBrowserRouter([
    {
        path: "bio",
        loader: bioLoader,
        element: <Bio />,
    },
    {
        path: "/",
        element: <Root />,
        errorElement: <ErrorPage />,
        children: [{
            errorElement: <ErrorPage />,
            children: [{
                index: true,
                loader: indexLoader,
                element: <Index />
            },
            {
                path: "matches/:matchId",
                loader: matchLoader,
                element: <Matches />,
            },
            {
                path: "pages/:pageId",
                loader: pageLoader,
                element: <Pages />,
            },
            {
                path: "attrezzatura",
                loader: accessoryLoader,
                element: <Accessories />,
            },
            {
                path: "sponsors",
                loader: sponsorsLoader,
                action: sponsorsAction,
                element: <Sponsors />,
            },
            {
                path: "calendar",
                loader: calendarLoader,
                element: <Calendar />,
            },
            {
                path: "cookie-policy",
                element: <CookiePolicy />,
            }]
        }],
    }
]);

const gaTrackingId = "G-ST9GQYL925";
if (window.location.hostname != "localhost") {
    ReactGA.initialize(gaTrackingId)
}

createRoot(document.getElementById('root')).render(
    <StrictMode>
        <BuyMeWidget />
        {/*https://www.freecodecamp.org/news/react-helmet-examples/*/}
        <HelmetProvider>
            <GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY}>
                <RouterProvider router={router}/>
            </GoogleReCaptchaProvider>
        </HelmetProvider>
    </StrictMode>,
)
