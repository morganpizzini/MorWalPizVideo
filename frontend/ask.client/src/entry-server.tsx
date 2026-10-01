import { renderToString } from "react-dom/server";
import {
  createStaticHandler,
  createStaticRouter,
  StaticRouterProvider,
} from "react-router";
import { HelmetProvider } from "react-helmet-async";
import { GoogleReCaptchaProvider } from "react-google-recaptcha-v3";
import { routes } from "./routes";
import "./styles.css";

function isResponseLike(value: unknown): value is Response {
  return (
    typeof value === "object" &&
    value !== null &&
    "status" in value &&
    typeof value.status === "number"
  );
}

function renderErrorPage(status: number, message: string) {
  return renderToString(
    <main role="alert">
      <h1>{status}</h1>
      <p>{message}</p>
    </main>,
  );
}

export async function render(
  request: Request,
): Promise<{ html: string; head: string; status: number }> {
  const handler = createStaticHandler(routes);
  let context: Awaited<ReturnType<typeof handler.query>>;
  try {
    context = await handler.query(request);
  } catch (error) {
    const status = isResponseLike(error) ? error.status : 500;
    const message =
      error instanceof Error ? error.message : "Unable to load this campaign.";
    return { html: renderErrorPage(status, message), head: "", status };
  }
  if (isResponseLike(context)) {
    return {
      html: renderErrorPage(
        context.status,
        context.statusText || `${context.status}`,
      ),
      head: "",
      status: context.status,
    };
  }
  const routeError = Object.values(context.errors ?? {})[0] as
    | { status?: number; statusText?: string; message?: string }
    | undefined;
  if (routeError) {
    const status = routeError.status ?? 500;
    const message =
      routeError.statusText ||
      routeError.message ||
      "Unable to load this campaign.";
    return { html: renderErrorPage(status, message), head: "", status };
  }
  const router = createStaticRouter(handler.dataRoutes, context);
  const helmetContext: {
    helmet?: import("react-helmet-async").HelmetServerState | null;
  } = {};
  const html = renderToString(
    <HelmetProvider
      context={
        helmetContext as {
          helmet?: import("react-helmet-async").HelmetServerState | null;
        }
      }
    >
      <GoogleReCaptchaProvider
        reCaptchaKey={import.meta.env.VITE_SITE_KEY ?? ""}
      >
        <StaticRouterProvider router={router} context={context} />
      </GoogleReCaptchaProvider>
    </HelmetProvider>,
  );
  const { helmet } = helmetContext;
  return {
    html,
    head: helmet
      ? `${helmet.title.toString()}${helmet.meta.toString()}${helmet.link.toString()}`
      : "",
    status: 200,
  };
}
