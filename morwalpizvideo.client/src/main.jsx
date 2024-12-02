import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './main.scss'
import Root from "./routes/root";
import Matches, { loader as matchLoader } from "./routes/matches";
import Pages, { loader as pageLoader } from "./routes/pages";
import ErrorPage from "./error-page";
import Accessories, { loader as accessoryLoader } from "./routes/accessories"
import Sponsors, { loader as sponsorsLoader } from "./routes/sponsors"
import Calendar, { loader as calendarLoader } from "./routes/calendar"
import Index, { loader as indexLoader } from "./routes/index";
import CookiePolicy from "./routes/cookie-policy";
import {
    createBrowserRouter,
    RouterProvider,
} from "react-router-dom";
import { HelmetProvider } from 'react-helmet-async';
import ReactGA from 'react-ga4';
// Import all of Bootstrap's JS
//import * as bootstrap from 'bootstrap'
//import Alert from 'bootstrap/js/dist/alert';

// or, specify which plugins you need:
//import { Tooltip, Toast, Popover } from 'bootstrap';

const router = createBrowserRouter([
    {
        path: "/",
        element: <Root />,
        errorElement: <ErrorPage />,
        //loader: rootLoader,
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
        {/*https://www.freecodecamp.org/news/react-helmet-examples/*/}
        <HelmetProvider>
            <RouterProvider router={router} />
        </HelmetProvider>
    </StrictMode>,
)
