import { test, expect } from "@playwright/test";
import { open } from "../helpers/app";

test.describe("Staff surfaces", () => {
  test("unauthenticated /admin does not open the dashboard", async ({ page }) => {
    await open(page, "/admin");
    await expect(page).not.toHaveURL(/\/admin$/);
    await expect(page.getByRole("heading", { name: /dashboard|overview/i })).toHaveCount(0);
  });

  test("GET staff ticket exchange is rejected", async ({ request }) => {
    const response = await request.get("/auth/complete?ticket=forged");
    expect(response.status()).toBe(405);
  });

  test("forged cookie sign-in is rejected", async ({ request, baseURL }) => {
    const response = await request.post("/auth/sign-in", {
      headers: {
        Origin: new URL(baseURL ?? "http://127.0.0.1:5276").origin,
        "Content-Type": "application/json"
      },
      data: {
        Token: "x",
        Role: "admin",
        UserId: "victim",
        Email: "attacker@example.com"
      }
    });
    expect(response.status()).toBe(401);
  });

  test("staff login URL is not linked from the public header", async ({ page }) => {
    await open(page, "/");
    await expect(page.locator('a[href*="f8e2a947-3c1b-4d9e-b562-7a0f1e8d3c92"]')).toHaveCount(0);
  });
});
