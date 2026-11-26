export interface WorkshopViteEnvironment {
  PORT?: string;
  ASPNETCORE_URLS?: string;
}

export interface WorkshopViteServerSettings {
  port: number;
  apiTarget: string;
}

const standalonePort = 5180;
const standaloneApiTarget = "https://localhost:7225";

function getPort(value: string | undefined): number {
  if (!value) return standalonePort;

  const port = Number(value);
  if (!Number.isInteger(port) || port < 1 || port > 65_535) {
    throw new Error(`PORT must be a valid TCP port; received "${value}".`);
  }
  return port;
}

function parseListenerUrl(value: string): URL | null {
  const wildcardHost = /^(https?:\/\/)(\*|\+|0\.0\.0\.0)(?=:)/i;
  try {
    return new URL(value.replace(wildcardHost, "$1localhost"));
  } catch {
    return null;
  }
}

function getApiTarget(value: string | undefined): string {
  if (!value) return standaloneApiTarget;

  const urls = value
    .split(";")
    .map((entry) => parseListenerUrl(entry.trim()))
    .filter((entry): entry is URL => entry !== null);
  const target = urls.find((entry) => entry.protocol === "https:") ?? urls[0];

  if (!target || (target.protocol !== "http:" && target.protocol !== "https:")) {
    throw new Error(
      "ASPNETCORE_URLS must contain an absolute HTTP or HTTPS API listener URL.",
    );
  }

  return target.origin;
}

export function getWorkshopViteServerSettings(
  environment: WorkshopViteEnvironment,
): WorkshopViteServerSettings {
  return {
    port: getPort(environment.PORT),
    apiTarget: getApiTarget(environment.ASPNETCORE_URLS),
  };
}
