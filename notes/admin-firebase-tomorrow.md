# Tomorrow: admin Firebase login (do not mix with shopper SPA)

Stop here for today. Shopper WASM is live. Admin is not finished.

## What is broken now

Signing in as `info@kobneti.com` on the admin login page accepts the Firebase password, then `/admin` shows the public 404.

That 404 is intentional when the `muuqwear_auth` cookie is missing. `/admin` is authorized InteractiveServer. Unsigned visitors are redirected to `/not-found` so the portal stays hidden.

After Firebase sign-in we currently put the Firebase ID token and refresh token into that cookie. Those tokens are large. The browser often drops the cookie, so the next request looks logged out.

## Do this in order

1. After Firebase password verify, set a **small** cookie (email, Firebase uid, role `admin`). Do not store the Firebase ID token in the cookie. Then `/admin` should open.
2. Browser-test: `info@kobneti.com` → dashboard. `mur7bac@gmail.com` → Access denied. Signed-out `/admin` still 404s.
3. Give admin API calls a token the API still accepts (short HMAC JWT, or verify Firebase ID tokens on the API). Dashboard widgets should load instead of 401.
4. Later, move admin orders/products/customers off Supabase onto Firestore. Separate sitting.

Verify step 1 on localhost, then deploy Cloud Run. Do not deploy until the dashboard actually opens.
