import { defineConfig } from "@playwright/test";

const baseURL = process.env.WORKSHOP_BASE_URL ?? "http://127.0.0.1:5180";
const parsedBaseURL = new URL(baseURL);
const baseUrlHost = parsedBaseURL.hostname;
const localHosts = new Set(["127.0.0.1", "localhost", "::1", "[::1]"]);

// Installing Chromium enables direct Playwright runs; it does not make a
// Copilot or other agent browser tool available.
if (
  !["http:", "https:"].includes(parsedBaseURL.protocol) ||
  parsedBaseURL.username ||
  parsedBaseURL.password ||
  !localHosts.has(baseUrlHost)
) {
  throw new Error(
    "Workshop browser verification only runs against a local base URL. " +
      "Set WORKSHOP_BASE_URL to a localhost or loopback address.",
  );
}

if (process.env.WORKSHOP_LOCAL_MOCK_API !== "1") {
  throw new Error(
    "Set WORKSHOP_LOCAL_MOCK_API=1 only after starting ServerAPI locally " +
      "with EnableMock=true in Development or Test.",
  );
}

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: "list",
  timeout: 90_000,
  use: {
    baseURL,
    browserName: "chromium",
    headless: true,
    locale: "en-GB",
    trace: "off",
    video: "off",
  },
});
