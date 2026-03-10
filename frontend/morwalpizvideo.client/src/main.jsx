import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './main.scss'
import Root from "./routes/root";
import ErrorPage from "./error-page";
import ErrorPageRoot from "./error-page-root";
import Bio from "./routes/bio"
import Matches from "./routes/matches";
import Pages from "./routes/pages";
import Links from "./routes/links";
import Accessories from "./routes/accessories"
import Sponsors from "./routes/sponsors"
import Calendar from "./routes/calendar"
import SponsorVideo from "./routes/sponsors-video";
import Index from "./routes/index";
import Compilations from "./routes/compilations";
import CustomForm from "./routes/customForm";
import bioLoader from "./routes/bio.loader"
import matchLoader from "./routes/matches.loader";
import pageLoader from "./routes/pages.loader";
import accessoryLoader from "./routes/accessories.loader";
import sponsorsLoader from "./routes/sponsors.loader";
import sponsorsAction from "./routes/sponsors.action";
import calendarLoader from "./routes/calendar.loader";
import streamLoader from "./routes/stream.loader";
import indexLoader from "./routes/index.loader";
import linktreeLoader from "./routes/linktree.loader";
import compilationsLoader from "./routes/compilations.loader";
import customFormLoader from "./routes/customForm.loader";
import ApiKeys from "./routes/apiKeys";
import apiKeysLoader from "./routes/apiKeys.loader";
import ApiKeyForm from "./routes/apiKeyForm";
import apiKeyFormLoader from "./routes/apiKeyForm.loader";
import ApiKeyDetail from "./routes/apiKeyDetail";
import apiKeyDetailLoader from "./routes/apiKeyDetail.loader";
import CookiePolicy from "./routes/cookie-policy";
import Bluetooth from "./routes/bluetooth";
import Stream from "./routes/stream";
import Linktree from "./routes/linktree";
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
import { registerSW } from 'virtual:pwa-register';

const router = createBrowserRouter([
    {
        path: "bio",
        loader: bioLoader,
        element: <Bio />,
        errorElement: <ErrorPageRoot />
    },
    {
        path: "compilations/:compilationUrl",
        loader: compilationsLoader,
        element: <Compilations />,
        errorElement: <ErrorPageRoot />
    },
    {
        path: "forms/:formUrl",
        loader: customFormLoader,
        element: <CustomForm />,
        errorElement: <ErrorPageRoot />
    },
    {
        path: "/",
        element: <Root />,
        errorElement: <ErrorPageRoot />,
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
                path: "bluetooth",
                    
                element: <Bluetooth />,
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
                path: "sponsors-video",
                element: <SponsorVideo />,
            },
            {
                path: "links",
                element: <Links />,
            },
            {
                path: "calendar",
                loader: calendarLoader,
                element: <Calendar />,
            },
            {
                path: "cookie-policy",
                element: <CookiePolicy />,
            },
            {
                path: "stream",
                loader: streamLoader,
                element: <Stream />,
            },
            {
                path: "linktree/:matchId",
                loader: linktreeLoader,
                element: <Linktree />,
            },
            {
                path: "apikeys",
                loader: apiKeysLoader,
                element: <ApiKeys />,
            },
            {
                path: "apikeys/create",
                loader: apiKeyFormLoader,
                element: <ApiKeyForm />,
            },
            {
                path: "apikeys/:id",
                loader: apiKeyDetailLoader,
                element: <ApiKeyDetail />,
            },
            {
                path: "apikeys/:id/edit",
                loader: apiKeyFormLoader,
                element: <ApiKeyForm />,
            }]
        }],
    }
]);

// Configurazione del registro SW con callback
const updateSW = registerSW({
    onNeedRefresh() {
        const userConsent = window.confirm(
            "Una nuova versione � disponibile. Vuoi aggiornare l'applicazione?"
        );
        if (userConsent) {
            updateSW();
        }
    },
    onOfflineReady() {
        //alert("L'app � pronta per funzionare offline.");
    },
});

const gaTrackingId = "G-ST9GQYL925";
if (window.location.hostname != "localhost") {
    ReactGA.initialize(gaTrackingId)
}

createRoot(document.getElementById('root')).render(
    <StrictMode>
        {/*https://www.freecodecamp.org/news/react-helmet-examples/*/}
        <HelmetProvider>
            <GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY}>
                <RouterProvider router={router} />
            </GoogleReCaptchaProvider>
        </HelmetProvider>
        <BuyMeWidget />
    </StrictMode>,
)
