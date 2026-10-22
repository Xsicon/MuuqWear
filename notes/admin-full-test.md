# Admin portal full test — 22 Oct 2026

Live site: `https://muuqwear.com/admin` as `info@kobneti.com` (Super Admin).

## Implemented (22 Oct)

1. Order details look up Firestore docs that are not GUIDs (`ABNWXSRK` / `orderNumber`). Notification and header search open `?order=`.
2. Returns, refunds, affiliates, content, live chat, system health/logs/jobs, badges, and careers now read Firestore (or local idle jobs) instead of the shopper API HMAC JWT.
3. Currency is `$`. Display name falls back to the email local-part when Firebase has no name / “Admin”.
4. Staff, sandbox, and `demo.muuqwear.test` shoppers are hidden from customers and orders (data kept).
5. Low-stock copy says a size is below 5 units, not total units.
6. Overview health no longer requires Supabase. System Health card is Firebase; integrations are Stripe / Firebase / Postmark.
7. Noise tickets/KB titles (`asda`, `mnbjca`, Test Question) are filtered in the UI.
8. Notification dropdown sits above tables. Search jumps to an order number with a full reload.

## Left as data (not deleted)

- Duplicate Warm Jacket SKUs.
- Many products Uncategorised / no size breakdown.
- Demo orders/customers remain in Firestore; they are filtered from admin lists.

Sign out and back in after deploy so the cookie picks up the display-name claim.
