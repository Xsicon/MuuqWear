import { test, expect } from "@playwright/test";
import { open } from "../helpers/app";

test.describe("Bag and checkout", () => {
  test("empty bag shows loading then empty state with a shop CTA", async ({ page }) => {
    await open(page, "/cart");
    await expect(page.getByRole("heading", { name: /your bag/i })).toBeVisible({ timeout: 45_000 });
    await expect(page.getByText(/your bag is empty|loading your bag/i)).toBeVisible();
    await expect(page.getByRole("link", { name: /shop now/i })).toBeVisible({ timeout: 20_000 });
    await page.getByRole("link", { name: /continue shopping/i }).click();
    await expect(page).toHaveURL(/\/shop\/apparel/);
  });

  test("empty checkout bounces back to the bag", async ({ page }) => {
    await open(page, "/checkout");
    await expect(page).toHaveURL(/\/cart/, { timeout: 45_000 });
    await expect(page.getByText(/your bag is empty/i)).toBeVisible();
  });

  test("shop to PDP to bag when catalog has a product", async ({ page }) => {
    await open(page, "/shop/apparel");
    await expect(page.locator(".mw-shop__title, h1")).toBeVisible({ timeout: 45_000 });
    const product = page.locator("a.mw-shop-product__body, a.mw-shop-product__media-link").first();
    const count = await page.locator("a.mw-shop-product__media-link").count();
    test.skip(count === 0, "Catalog is empty in this environment");
    await product.click();
    await expect(page).toHaveURL(/\/productdetail\//);
    const add = page.getByRole("button", { name: /add to cart/i });
    if (await add.isDisabled()) {
      await expect(page.getByText(/sold out/i)).toBeVisible();
      return;
    }
    await add.click();
    await page.getByRole("button", { name: /bag|cart/i }).first().click();
    await expect(page).toHaveURL(/\/cart/);
    await expect(page.getByRole("link", { name: /checkout securely/i })).toBeVisible();
    await page.getByRole("link", { name: /checkout securely/i }).click();
    await expect(page).toHaveURL(/\/checkout/);
    await expect(page.getByRole("heading", { name: "Checkout" })).toBeVisible();
    await page.getByRole("button", { name: /continue to payment/i }).click();
    await expect(page.getByText(/email is required|first name is required/i)).toBeVisible();
  });
});
