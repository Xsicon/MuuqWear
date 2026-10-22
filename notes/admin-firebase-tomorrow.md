# Admin Firebase login, then MuuqWear off Doha

In progress this session (was parked overnight).

## Order

1. Small admin cookie (HMAC JWT, no Firebase ID token).
2. Browser-test `info@kobneti.com` dashboard, `mur7bac@gmail.com` access denied, signed-out `/admin` 404.
3. HMAC JWT is the API bearer; admin products/orders/customers read Firestore.
4. Redeploy `muuqwear-web` and `muuqwear-api` to `europe-west2`. Kobneti stays in Doha. Do not touch the Kobneti project.

Do not print secrets. Sign in only as `info@kobneti.com`.
