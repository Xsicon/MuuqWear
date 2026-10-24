import { test, expect } from "@playwright/test";
import { hasShopperCredentials, open, signInWithPassword } from "../helpers/app";

test.describe("Profile and settings", () => {
  test("anonymous profile visits redirect to login", async ({ page }) => {
    await open(page, "/profile");
    await expect(page).toHaveURL(/\/login/, { timeout: 45_000 });
  });

  test("anonymous settings and account aliases also require sign-in", async ({ page }) => {
    await open(page, "/settings");
    await expect(page).toHaveURL(/\/login/, { timeout: 45_000 });
    await open(page, "/account");
    await expect(page).toHaveURL(/\/login/, { timeout: 45_000 });
  });

  test("signed-in shopper can switch sidebar tabs", async ({ page }) => {
    test.skip(!hasShopperCredentials(), "Set MUUQ_E2E_EMAIL and MUUQ_E2E_PASSWORD");
    await signInWithPassword(page, process.env.MUUQ_E2E_EMAIL!, process.env.MUUQ_E2E_PASSWORD!);
    await expect(page).toHaveURL(/\/profile/, { timeout: 30_000 });
    await page.locator("nav.mw-profile-nav a", { hasText: "Orders" }).click();
    await expect(page.getByText("All Orders")).toBeVisible({ timeout: 15_000 });
    await page.locator("nav.mw-profile-nav a", { hasText: "Settings" }).click();
    await expect(page.getByText("Full Name", { exact: true })).toBeVisible({ timeout: 15_000 });
  });
});
