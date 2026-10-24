import { expect, type Page } from "@playwright/test";

export async function open(page: Page, path = "/") {
  try {
    await page.goto(path, { waitUntil: "domcontentloaded" });
  } catch (error) {
    const text = String(error);
    if (!text.includes("HTTP_RESPONSE_CODE_FAILURE") && !text.includes("net::ERR"))
      throw error;
  }
  await page.waitForSelector("body", { timeout: 60_000 });
  await page.waitForSelector("html[data-mw-ready='1']", { timeout: 60_000 });
}

export async function waitForLoginReady(page: Page) {
  await page.waitForSelector("#mw-login-ready", { state: "attached", timeout: 45_000 });
}

export async function waitForRegisterReady(page: Page) {
  await page.waitForSelector("#mw-register-ready", { state: "attached", timeout: 45_000 });
}

export async function waitForShopperChrome(page: Page) {
  await expect(page.locator(".mw-header, [role='banner']").first()).toBeVisible({ timeout: 45_000 });
  await expect(page.getByRole("link", { name: /muuqwear/i }).first()).toBeVisible();
}

export async function signInWithPassword(page: Page, email: string, password: string) {
  await open(page, "/login");
  await expect(page.locator(".mw-auth-title")).toBeVisible({ timeout: 45_000 });
  await page.waitForSelector("#mw-login-ready", { state: "attached", timeout: 45_000 });
  await expect(page.getByRole("button", { name: "Login" })).toBeEnabled();
  await page.getByPlaceholder("Email").fill(email);
  await page.getByPlaceholder("Password").fill(password);
  await page.getByRole("button", { name: "Login" }).click();
}

export function hasShopperCredentials() {
  return !!(process.env.MUUQ_E2E_EMAIL && process.env.MUUQ_E2E_PASSWORD);
}
