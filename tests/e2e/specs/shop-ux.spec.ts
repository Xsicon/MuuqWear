import { test, expect } from "@playwright/test";
import { open, waitForShopperChrome } from "../helpers/app";

test.describe("Shop UX", () => {
  test("apparel shows a count and filter controls", async ({ page }) => {
    await open(page, "/shop/apparel");
    await waitForShopperChrome(page);
    await expect(page.getByRole("button", { name: /filters/i })).toBeVisible();
    await expect(page.getByText(/showing/i)).toBeVisible({ timeout: 30_000 });
    await expect(page.getByLabel("Sort products")).toBeVisible();
  });

  test("accessories route is distinct from apparel", async ({ page }) => {
    await open(page, "/shop/accessories");
    await waitForShopperChrome(page);
    await expect(page).toHaveURL(/\/shop\/accessories/);
    await expect(page.getByRole("heading", { name: "Accessories" })).toBeVisible();
  });

  test("invalid affiliate ref does not break the home page", async ({ page }) => {
    await open(page, "/?ref=NOTAREALCODE999");
    await waitForShopperChrome(page);
    await expect(page.getByRole("banner")).toBeVisible();
  });

  test("wasm smoke page confirms client render", async ({ page }) => {
    await open(page, "/wasm-smoke");
    await expect(page.getByRole("heading", { name: /webassembly is running/i })).toBeVisible({ timeout: 45_000 });
  });
});
