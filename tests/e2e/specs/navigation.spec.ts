import { test, expect } from "@playwright/test";
import { open, waitForShopperChrome } from "../helpers/app";

test.describe("Storefront navigation", () => {
  test("home renders brand chrome and primary destinations", async ({ page }) => {
    await open(page, "/");
    await waitForShopperChrome(page);
    await expect(page.getByRole("navigation").getByRole("link", { name: "Home" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Apparel" }).first()).toBeVisible();
    await expect(page.getByRole("link", { name: "Accessories" }).first()).toBeVisible();
    await expect(page.getByRole("link", { name: "Journal" }).first()).toBeVisible();
    await expect(page.getByRole("link", { name: "Sign In" }).first()).toBeVisible();
  });

  test("header search goes to the shop, bag goes to cart", async ({ page }) => {
    await open(page, "/");
    await waitForShopperChrome(page);
    await page.getByRole("link", { name: "Search" }).first().click();
    await expect(page).toHaveURL(/\/shop\/?/);
    await open(page, "/");
    await page.getByRole("link", { name: /bag|cart/i }).first().click();
    await expect(page).toHaveURL(/\/cart/);
    await expect(page.getByRole("heading", { name: /your bag/i })).toBeVisible();
  });

  test("footer company and legal links load without a 404", async ({ page }) => {
    const paths = [
      "/about",
      "/faq",
      "/privacy",
      "/terms",
      "/size-guide",
      "/contact",
      "/journal",
      "/help",
      "/shipping-return",
      "/careers",
      "/design-history",
      "/affiliate"
    ];
    for (const path of paths) {
      await open(page, path);
      await expect(page.getByRole("heading", { name: /that page is not here/i })).toHaveCount(0);
      await expect(page.locator("body")).not.toContainText("An unhandled exception");
    }
  });

  test("unknown shopper routes show the WASM not-found copy", async ({ page }) => {
    await open(page, "/this-route-does-not-exist");
    await expect(page.getByRole("heading", { name: /that page is not here/i })).toBeVisible();
    await page.getByRole("link", { name: /back to home/i }).click();
    await expect(page).toHaveURL(/\/$/);
  });

  test("community is a full document navigation off WASM", async ({ page }) => {
    await open(page, "/");
    await waitForShopperChrome(page);
    await page.getByRole("link", { name: "Community" }).first().click();
    await expect(page).toHaveURL(/\/community/);
  });
});
