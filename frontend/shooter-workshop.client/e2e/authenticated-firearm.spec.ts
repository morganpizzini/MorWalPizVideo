import { randomBytes, randomUUID } from "node:crypto";
import { mkdir } from "node:fs/promises";
import { tmpdir } from "node:os";
import path from "node:path";
import { expect, test } from "@playwright/test";

test("registers, signs in, and captures an account-owned firearm detail", async ({
  page,
  baseURL,
}) => {
  if (!baseURL) throw new Error("Playwright baseURL is required.");

  const runId = randomUUID().replaceAll("-", "");
  const username = `browser${runId}`.slice(0, 40);
  const email = `workshop-${runId}@example.invalid`;
  const password = `Workshop-${randomBytes(18).toString("base64url")}-A1!`;
  const firearmNickname = `Browser check ${runId.slice(0, 8)}`;
  const screenshotPath = path.resolve(
    process.env.WORKSHOP_SCREENSHOT_PATH ??
      path.join(tmpdir(), "workshop-authenticated-firearm.png"),
  );

  const anonymousResponse = await page.request.get("/api/v1/workshop/firearms");
  expect(anonymousResponse.status()).toBe(401);

  await page.goto("/account/register?returnUrl=%2Ffirearms");
  await expect(
    page.getByRole("heading", { name: "Create your Workshop account" }),
  ).toBeVisible();
  await page.getByLabel("Username", { exact: true }).fill(username);
  await page.getByLabel("Email", { exact: true }).fill(email);
  await page
    .getByLabel("Display name", { exact: true })
    .fill(`Workshop browser ${runId.slice(0, 8)}`);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByLabel("Confirm password", { exact: true }).fill(password);
  await page
    .getByLabel("I have read the privacy notice and accept the terms of use.")
    .check();

  const registrationResponse = page.waitForResponse(
    (response) =>
      response.url().includes("/api/v1/accounts") &&
      response.request().method() === "POST",
  );
  await page.getByRole("button", { name: "Create account" }).click();
  expect((await registrationResponse).status()).toBe(200);
  await expect(page).toHaveURL(/\/account\/login\?/);

  await page.getByLabel("Username or email", { exact: true }).fill(username);
  await page.getByLabel("Password", { exact: true }).fill(password);
  const loginResponse = page.waitForResponse(
    (response) =>
      response.url().includes("/api/v1/account/sessions") &&
      response.request().method() === "POST",
  );
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  expect((await loginResponse).status()).toBe(200);
  await expect(page).toHaveURL(/\/firearms$/);
  const firstUseGuide = page.getByRole("dialog", { name: "Welcome to Workshop" });
  await expect(firstUseGuide).toBeVisible();
  await firstUseGuide.getByRole("button", { name: "Skip for now" }).click();
  await expect(firstUseGuide).toBeHidden();

  const sessionCookies = await page.context().cookies(baseURL);
  expect(
    sessionCookies.some(
      (cookie) => cookie.name === "public_account_session" && cookie.httpOnly,
    ),
  ).toBe(true);

  await page.getByRole("button", { name: /Register firearm/ }).click();
  await page.getByLabel("manufacturer", { exact: true }).fill("Browser Test Arms");
  await page.getByLabel("model", { exact: true }).fill(`Model ${runId.slice(0, 8)}`);
  await page.getByLabel("caliber", { exact: true }).fill("Test caliber");
  await page.getByLabel("nickname", { exact: true }).fill(firearmNickname);
  await page.getByLabel("Initial rounds", { exact: true }).fill("137");
  const createFirearmResponsePromise = page.waitForResponse(
    (response) =>
      response.url().includes("/api/v1/workshop/firearms") &&
      response.request().method() === "POST",
  );
  await page.getByRole("button", { name: "Save firearm" }).click();
  const createFirearmResponse = await createFirearmResponsePromise;
  expect(createFirearmResponse.status()).toBe(200);
  expect(new URL(createFirearmResponse.url()).origin).toBe(new URL(baseURL).origin);

  const firearmLink = page.getByRole("link").filter({ hasText: firearmNickname });
  await expect(firearmLink).toBeVisible();
  const detailResponsePromise = page.waitForResponse(
    (response) => {
      const responsePath = new URL(response.url()).pathname;
      return (
        responsePath.includes("/api/v1/workshop/firearms/") &&
        responsePath.endsWith("/detail") &&
        response.request().method() === "GET"
      );
    },
  );
  await firearmLink.click();

  await expect(page).toHaveURL(/\/firearms\/[^/]+$/);
  const detailResponse = await detailResponsePromise;
  expect(detailResponse.status()).toBe(200);
  expect(new URL(detailResponse.url()).origin).toBe(new URL(baseURL).origin);
  await expect(
    page.getByRole("heading", { level: 1, name: firearmNickname }),
  ).toBeVisible();
  await expect(page.getByText("Test caliber", { exact: true })).toBeVisible();
  await expect(
    page.locator(".stat").filter({ hasText: "Total rounds" }).getByText("137", {
      exact: true,
    }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Installed components" }),
  ).toBeVisible();

  await mkdir(path.dirname(screenshotPath), { recursive: true });
  await page.screenshot({ path: screenshotPath, fullPage: true });
  test.info().annotations.push({
    type: "screenshot",
    description: screenshotPath,
  });
});
