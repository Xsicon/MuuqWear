# Admin RBAC — API alignment

> **Status:** Complete (API v1 aligned with Web v1)  
> **Last updated:** 2026-07-20  
> **Related:** [ADMIN_RBAC_PLAN.md](./ADMIN_RBAC_PLAN.md) · API repo `docs/API_RBAC.md`

The Blazor admin portal and external API both enforce section-level RBAC. Policy names and role keys match between layers.

## Web integration (this repo)

| Check | Status | Location |
|-------|--------|----------|
| Role stored as `ClaimTypes.Role` in auth cookie | Verified | `CookieAuthHelper.CreatePrincipal` |
| Bearer token sent on API calls | Verified | `AuthenticatedHttpHandler` (`AccessToken` claim) |
| Admin login allows 6 staff roles only | Verified | `AdminLoginComponent.razor` |
| Storefront login rejects non-`user` roles | Verified | `LoginComponent.razor` |
| Section policies on all admin pages | Verified | `AdminPortalPolicies` + page `[Authorize]` |
| 403 JSON surfaced in UI | Verified | `ApiErrorMessageHelper` / `AdminUiErrorHelper` |
| Role synced on token refresh | Verified | `TokenRefreshMiddleware`, `AuthenticatedHttpHandler` |

## API (external — implemented)

| Item | Status | Notes |
|------|--------|-------|
| Custom HS256 JWT with `app_role` claim | Done | Minted on login, OTP, magic link, refresh |
| Section policies (`AdminOrders`, `AdminProducts`, …) | Done | `AdminAuthorizationServiceCollectionExtensions.cs` |
| `AdminCustomerNotesRead` (admin + support_team) | Done | Customer list + notes GET |
| 403 JSON `{ success: false, message: "..." }` | Done | `ForbiddenJsonAuthorizationMiddlewareResultHandler` |
| Demo users seeded | Done | `docs/scripts/seed-demo-admin-users.ps1` |
| Smoke tests | Done | `docs/scripts/rbac-smoke-test.ps1` |

### Policy matrix (API ↔ Web)

| Policy | Roles |
|--------|-------|
| StaffPortal | All 6 staff |
| AdminOrders | admin, operations_manager |
| AdminProducts | admin, merchandising |
| AdminContent | admin, content_team |
| AdminAffiliates | admin, operations_manager |
| AdminSupport | admin, support_team |
| AdminCareers | admin, operations_manager |
| AdminSystem | admin, technology_systems |
| AdminCustomers | admin only |
| AdminCustomerNotesRead | admin, support_team |
| AdminAnalytics | admin, operations_manager |
| AdminOnly | admin only |

## Role keys (must match JWT / login response)

- `admin`
- `operations_manager`
- `support_team`
- `merchandising`
- `content_team`
- `technology_systems`

## Post-deploy checklist

1. **Restart the API** and rebuild so JWT + policies load.
2. **Re-login** in the admin portal — old Supabase-only tokens lack `app_role`.
3. Run **`docs/scripts/rbac-smoke-test.ps1`** in the API repo against each demo role.
4. Run **Phase 8** manual QA in [ADMIN_RBAC_PLAN.md](./ADMIN_RBAC_PLAN.md) §6.

## Storefront stays fully public

RBAC applies to the **admin portal only**. The customer-facing storefront must remain anonymous — no auth required to browse or search products.

**Do not protect storefront catalog reads.** These endpoints must stay `[AllowAnonymous]` on the API (same as before RBAC):

| Endpoint | Used by (Web) |
|----------|----------------|
| `GET /api/Product/all` | Apparel, Accessories, nav search |
| `GET /api/Product/home` | Home page |
| `GET /api/Product/{id}` | Product detail |
| `GET /api/Product/{id}/related` | Related products |

`AdminProducts` applies to **admin mutations and staff catalog management** (add, update, delete, stock, upload) — not anonymous storefront browse/search.

**No Web changes needed** for public catalog; keep using `ProductService.GetAll()` and existing storefront pages as-is.
