import Root from "./layout/root";
import ErrorPage from "../error-page";
import ErrorPageRoot from "../error-page-root";
import Matches from "./matches/Component";
import Pages from "./pages/Component";
import Links from "./links/Component";
import Accessories from "./accessories/Component";
import Sponsors from "./sponsors/Component";
import Calendar from "./calendar/Component";
import SponsorVideo from "./system/sponsors-video";
import Index from "./home/Component";
import Compilations from "./compilations/Component";
import CustomForm from "./customForms/Component";
import matchLoader from "./matches/loader";
import pageLoader from "./pages/loader";
import accessoryLoader from "./accessories/loader";
import sponsorsLoader from "./sponsors/loader";
import sponsorsAction from "./sponsors/action";
import calendarLoader from "./calendar/loader";
import streamLoader from "./stream/loader";
import compilationsLoader from "./compilations/loader";
import customFormLoader from "./customForms/loader";
import CookiePolicy from "./system/cookie-policy";
import Bluetooth from "./system/bluetooth";
import Stream from "./stream/Component";
import QuickLinks from "./quickLinks/Component";
import quickLinksLoader from "./quickLinks/loader";
import ChannelNews from "./channelNews/Component";
import channelNewsLoader from "./channelNews/loader";
import type { RouteObject } from "react-router";

export const routes: RouteObject[] = [
    {
        path: "/quick-links/:url",
        loader: quickLinksLoader,
        element: <QuickLinks />,
        errorElement: <ErrorPageRoot />,
    },
    {
        path: "/channel-news/:idOrSlug",
        loader: channelNewsLoader,
        element: <ChannelNews />,
        errorElement: <ErrorPageRoot />,
    },
    {
        path: "compilations/:compilationUrl",
        loader: compilationsLoader,
        element: <Compilations />,
        errorElement: <ErrorPageRoot />,
    },
    {
        path: "forms/:formUrl",
        loader: customFormLoader,
        element: <CustomForm />,
        errorElement: <ErrorPageRoot />,
    },
    {
        path: "/",
        element: <Root />,
        errorElement: <ErrorPageRoot />,
        children: [
            {
                errorElement: <ErrorPage />,
                children: [
                    {
                        index: true,
                        element: <Index />,
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
                ],
            },
        ],
    },
];
