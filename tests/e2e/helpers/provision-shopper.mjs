import { readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const settings = JSON.parse(readFileSync(resolve(root, "MuuqWear.Web/appsettings.json"), "utf8"));
const apiKey = settings?.Firebase?.ApiKey;
if (!apiKey) {
  console.error("Firebase API key missing from appsettings.json");
  process.exit(1);
}

const stamp = Date.now().toString(36);
const email = `e2e.shopper.${stamp}@gmail.com`;
const password = `E2ePass1!${stamp.slice(-4)}`;
const response = await fetch(
  `https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=${encodeURIComponent(apiKey)}`,
  {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password, returnSecureToken: true, displayName: "E2E Shopper" })
  }
);
const body = await response.json();
if (!response.ok || !body.localId) {
  console.error(body?.error?.message || "Firebase sign-up failed");
  process.exit(1);
}

writeFileSync(resolve(dirname(fileURLToPath(import.meta.url)), "../.e2e-user"), `${email}\n${password}\n`, { mode: 0o600 });
console.log("provisioned");
