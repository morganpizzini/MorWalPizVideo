import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './main.scss'
import Root from "./routes/root";
import Matches, { loader as matchLoader } from "./routes/matches";
import ErrorPage from "./error-page";
import Accessories, { loader as accessoryLoader } from "./routes/accessories"
import Sponsors, { loader as sponsorsLoader } from "./routes/sponsors"
import Index, { loader as indexLoader } from "./routes/index";
import CookiePolicy from "./routes/cookie-policy";
import {
    createBrowserRouter,
    RouterProvider,
} from "react-router-dom";
import { HelmetProvider } from 'react-helmet-async';
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
                path: "cookie-policy",
                element: <CookiePolicy />,
            }]
        }],
    }
]);

createRoot(document.getElementById('root')).render(
    <StrictMode>
        {/*https://www.freecodecamp.org/news/react-helmet-examples/*/}
        <HelmetProvider>
            <RouterProvider router={router} />
        </HelmetProvider>
    </StrictMode>,
)
