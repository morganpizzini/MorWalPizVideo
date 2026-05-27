import Root from "./root";
import ErrorPage from "../error-page";
import ErrorPageRoot from "../error-page-root";
import Bio from "./bio";
import Matches from "./matches";
import Pages from "./pages";
import Links from "./links";
import Accessories from "./accessories";
import Sponsors from "./sponsors";
import Calendar from "./calendar";
import SponsorVideo from "./sponsors-video";
import Index from "./index";
import Compilations from "./compilations";
import CustomForm from "./customForm";
import bioLoader from "./bio.loader";
import matchLoader from "./matches.loader";
import pageLoader from "./pages.loader";
import accessoryLoader from "./accessories.loader";
import sponsorsLoader from "./sponsors.loader";
import sponsorsAction from "./sponsors.action";
import calendarLoader from "./calendar.loader";
import streamLoader from "./stream.loader";
import indexLoader from "./index.loader";
import linktreeLoader from "./linktree.loader";
import compilationsLoader from "./compilations.loader";
import customFormLoader from "./customForm.loader";
import ApiKeys from "./apiKeys";
import apiKeysLoader from "./apiKeys.loader";
import ApiKeyForm from "./apiKeyForm";
import apiKeyFormLoader from "./apiKeyForm.loader";
import ApiKeyDetail from "./apiKeyDetail";
import apiKeyDetailLoader from "./apiKeyDetail.loader";
import CookiePolicy from "./cookie-policy";
import Bluetooth from "./bluetooth";
import Stream from "./stream";
import Linktree from "./linktree";
import type { RouteObject } from "react-router";

export const routes: RouteObject[] = [
    {
        path: "bio",
        loader: bioLoader,
        element: <Bio />,
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
                        loader: indexLoader,
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
                    },
                ],
            },
        ],
    },
];
