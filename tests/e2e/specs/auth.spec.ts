import { test, expect } from "@playwright/test";
import { hasShopperCredentials, open, signInWithPassword, waitForLoginReady, waitForRegisterReady } from "../helpers/app";

test.describe("Account registration and sign-in", () => {
  test("empty login submit shows field validation, not a hang", async ({ page }) => {
    await open(page, "/login");
    await expect(page.locator(".mw-auth-title")).toBeVisible({ timeout: 45_000 });
    await waitForLoginReady(page);
    await expect(page.getByPlaceholder("Email")).toBeEnabled();
    await page.getByRole("button", { name: "Login" }).click();
    await expect(page.getByText("Email is required")).toBeVisible();
    await expect(page.getByText("Password is required")).toBeVisible();
    await expect(page.getByRole("button", { name: "Login" })).toBeEnabled();
  });

  test("malformed email and weak password stay on login with messages", async ({ page }) => {
    await open(page, "/login");
    await waitForLoginReady(page);
    await page.getByPlaceholder("Email").fill("not-an-email");
    await page.getByPlaceholder("Password").fill("short");
    await page.getByRole("button", { name: "Login" }).click();
    await expect(page.getByText(/invalid email format/i)).toBeVisible();
    await expect(page).toHaveURL(/\/login/);
  });

  test("wrong password of valid shape surfaces an error and restores the button", async ({ page }) => {
    await open(page, "/login");
    await waitForLoginReady(page);
    await page.getByPlaceholder("Email").fill("nobody@example.com");
    await page.getByPlaceholder("Password").fill("WrongPass1!");
    await page.getByRole("button", { name: "Login" }).click();
    await expect(page.getByRole("button", { name: "Login" })).toBeEnabled({ timeout: 20_000 });
    await expect(page.locator(".text-danger, .mw-alert-error, .validation-message")).not.toHaveCount(0);
    await expect(page).toHaveURL(/\/login/);
  });

  test("magic link tab validates empty and invalid emails", async ({ page }) => {
    await open(page, "/login");
    await waitForLoginReady(page);
    await page.getByRole("button", { name: "Magic Link" }).click();
    await page.getByRole("button", { name: "Send Magic Link" }).click();
    await expect(page.getByText("Please enter your email")).toBeVisible();
    await page.getByPlaceholder("Enter your email address").fill("nope");
    await page.getByRole("button", { name: "Send Magic Link" }).click();
    await expect(page.getByText("Please enter a valid email address")).toBeVisible();
  });

  test("register empty submit and password mismatch", async ({ page }) => {
    await open(page, "/register");
    await expect(page.locator(".mw-auth-title")).toBeVisible({ timeout: 45_000 });
    await waitForRegisterReady(page);
    await page.getByRole("button", { name: "Create Account" }).click();
    await expect(page.getByText("Full name is required")).toBeVisible();
    await expect(page.getByText("Email is required")).toBeVisible();

    await page.locator("label", { hasText: "Full Name" }).locator("..").locator("input").fill("Test Shopper");
    await page.locator("label", { hasText: "Email" }).locator("..").locator("input").fill("qa@example.com");
    await page.locator('input[type="password"]').first().fill("ValidPass1!");
    await page.locator("label", { hasText: "Confirm Password" }).locator("..").locator("input").fill("OtherPass1!");
    await page.getByRole("button", { name: "Create Account" }).click();
    await expect(page.getByText("Passwords do not match")).toBeVisible();
    await expect(page.getByRole("button", { name: "Create Account" })).toBeEnabled();
  });

  test("login and register cross-link, forgot-password is reachable", async ({ page }) => {
    await open(page, "/login");
    await page.getByRole("link", { name: "Create one" }).click();
    await expect(page).toHaveURL(/\/register/);
    await page.getByRole("link", { name: "Sign in" }).click();
    await expect(page).toHaveURL(/\/login/);
    await page.getByRole("link", { name: "Forgot password?" }).click();
    await expect(page).toHaveURL(/\/forgot-password/);
    await expect(page.getByRole("heading", { name: /forgot password/i })).toBeVisible();
  });

  test("expired session query shows a warning on login", async ({ page }) => {
    await open(page, "/login?expired=true");
    await expect(page.getByText(/session has expired/i)).toBeVisible();
  });

  test("signed-in shopper reaches profile when credentials are provided", async ({ page }) => {
    test.skip(!hasShopperCredentials(), "Set MUUQ_E2E_EMAIL and MUUQ_E2E_PASSWORD");
    await signInWithPassword(page, process.env.MUUQ_E2E_EMAIL!, process.env.MUUQ_E2E_PASSWORD!);
    await expect(page).toHaveURL(/\/profile/, { timeout: 30_000 });
    await expect(page.locator(".mw-profile-nav a.active")).toContainText("Overview");
  });
});
