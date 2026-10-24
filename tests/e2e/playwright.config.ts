import { defineConfig, devices } from "@playwright/test";
import path from "path";

const port = process.env.E2E_PORT ?? "5276";
const baseURL = process.env.E2E_BASE_URL ?? `http://127.0.0.1:${port}`;
const webProject = path.resolve(__dirname, "../../MuuqWear.Web/MuuqWear.Web.csproj");

export default defineConfig({
  testDir: "./specs",
  testIgnore: ["**/debug-login.spec.ts"],
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL,
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "off",
    viewport: { width: 1280, height: 800 }
  },
  webServer: process.env.E2E_SKIP_WEBSERVER
    ? undefined
    : {
        command: `dotnet run --project "${webProject}" --urls ${baseURL}`,
        url: baseURL,
        reuseExistingServer: !process.env.CI,
        timeout: 180_000
      },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"], channel: "chrome" } }
  ]
});
