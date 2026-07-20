# Admin RBAC — API alignment gaps

> **Status:** Documented (Option A — frontend-first)  
> **Last updated:** 2026-07-20  
> **Related:** [ADMIN_RBAC_PLAN.md](./ADMIN_RBAC_PLAN.md)

The Blazor admin portal enforces section-level RBAC in the UI (nav, routes, dashboard, notifications). The external API may still require the `admin` role on many endpoints until policies are updated server-side.

## Verified in this repo (Web)

| Check | Status | Location |
|-------|--------|----------|
| Role stored as `ClaimTypes.Role` in auth cookie | Verified | `CookieAuthHelper.CreatePrincipal` |
| Admin login allows 6 staff roles only | Verified | `AdminLoginComponent.razor` |
| Storefront login rejects non-`user` roles | Verified | `LoginComponent.razor` (`Role != "user"`) |
| Section policies on all admin pages | Verified | `AdminPortalPolicies` + page `[Authorize]` |

## Pending (external API)

| Item | Owner | Notes |
|------|-------|-------|
| Demo users seeded with correct roles/passwords | API / ops | See demo table in `ADMIN_RBAC_PLAN.md` §4 |
| Endpoint policies match §4 access matrix | API team | Option B in plan Phase 0.3 |
| Non-admin data calls return 403 until aligned | Expected | UI shows friendly message via `AdminUiErrorHelper` |

## Role keys (must match JWT / login response)

- `admin`
- `operations_manager`
- `support_team`
- `merchandising`
- `content_team`
- `technology_systems`

## Recommended API policy mapping (v1)

| API area (typical) | Allowed roles |
|--------------------|---------------|
| Orders / returns / refunds | `admin`, `operations_manager` |
| Customers / notes | `admin` *(Support: notes feed only in UI today)* |
| Products / stock | `admin`, `merchandising` |
| Content | `admin`, `content_team` |
| Affiliates | `admin`, `operations_manager` |
| Support / tickets / chat | `admin`, `support_team` |
| Careers / jobs | `admin`, `operations_manager` |
| System / health / logs | `admin`, `technology_systems` |

Until API policies match, non-admin staff may see empty panels or permission errors on allowed pages. Route and nav guards still prevent browsing forbidden sections.
