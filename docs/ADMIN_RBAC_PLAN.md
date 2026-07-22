# Admin RBAC — Implementation Plan

> **Status:** Complete (Web v1 + API v1) — run Phase 8 manual QA after API restart and re-login; see [ADMIN_RBAC_API_GAPS.md](./ADMIN_RBAC_API_GAPS.md)  
> **Last updated:** 2026-07-20  
> **Related:** [ADMIN_REDESIGN.md](./ADMIN_REDESIGN.md) §4 Role-based access  
> **Branch context:** `feature/admin`

This document is the single source of truth for implementing **role-based access control** in the MuuqWear admin portal. Use it before starting RBAC work in Blazor or the API.

---

## 1. Current state (as of Phase 7)

| Layer | Role-based? | What actually happens |
|-------|-------------|------------------------|
| API login / JWT | Yes | Custom JWT with `app_role`; login response `data.role` matches |
| Admin login gate | Yes | 6 staff roles allowed; redirects to role-specific home |
| Admin pages | Yes | Section policies per route; forbidden URLs → access denied |
| Sidebar / dashboard | Yes | Nav, overview, and badges filtered by role |
| Header notifications | Yes | Orders, affiliates, notes, low stock scoped by role |
| API data calls | Yes | Section policies on admin endpoints; 403 JSON in UI |
| Demo credentials | Dev-only | Password table hidden outside Development |

**Bottom line:** Web and API RBAC are aligned for v1 admin routes. **Storefront catalog stays fully public** — API must not require auth on `GET /api/Product/all`, `/home`, or `/{id}`. Restart API, re-login in admin, then run Phase 8 manual QA.

### Automated coverage

- `MuuqWear.Tests/Admin/AdminPortalRbacTests.cs` — access matrix, route mapping, notification access

### Key files today

| Area | Path |
|------|------|
| Role allow-list | `MuuqWear.Web/Constants/AdminPortalRoles.cs` |
| Admin login | `MuuqWear.Web/Components/Pages/AdminComponent/AdminLoginComponent.razor` |
| Storefront login | `MuuqWear.Web/Components/Pages/LoginComponent/LoginComponent.razor` |
| Cookie / claims | `MuuqWear.Application/Shared/CookieAuthHelper.cs` |
| Nav (filtered by role) | `MuuqWear.Web/Components/Layout/AdminLayoutComponent/AdminNavMenuComponent.razor` |
| Dashboard (role-aware) | `MuuqWear.Web/Components/Pages/AdminComponent/AdminDashboardComponent.razor` |
| Notifications feed | `MuuqWear.Web/Services/AdminHeaderNotificationFeedBuilder.cs` |
| Figma role map | `docs/ADMIN_REDESIGN.md` §4 |

---

## 2. Goal

Turn the current “any staff role → full admin” portal into **true role-based access**:

- Each role sees **only their sections** in nav, dashboard, and notifications
- Each role can **only open** their allowed routes
- Behavior matches Figma + demo accounts on admin login
- Persists correctly after logout/login
- Aligns with API authorization where possible (frontend-only RBAC is not sufficient security)

---

## 3. Decisions to lock before coding

| # | Decision | Recommended default | Confirmed? |
|---|----------|---------------------|------------|
| D1 | **Customers** — not in Figma role map, but exists in nav | **Admin only** for `/admin/customers`. Support sees customer **notes** in header/messages only (no Customers page access until expanded). | ☑ |
| D2 | **Overview (`/admin`)** | All staff roles can open Overview; stats / “Your Access” / quick actions are **filtered by role** | ☑ |
| D3 | **Denied route UX** | Dedicated `/admin/access-denied` (prefer over silent `/not-found`) | ☑ |
| D4 | **API role keys** | Must match exactly: `admin`, `operations_manager`, `support_team`, `merchandising`, `content_team`, `technology_systems` | ☑ |
| D5 | **Scope v1** | **Section-level RBAC only** (hide whole nav groups / pages). Not button-level permissions inside a page | ☑ |
| D6 | **Demo accounts table** | Shown on admin login in **Development only**; hidden in Staging/Production | ☑ |

### Resolved (v1)

1. **Customers** — Admin only for `/admin/customers`; Support gets header/messages notes only (D1).
2. **Returns/refunds** — Follow Orders section (ops + admin).
3. **Forbidden URLs** — `/admin/access-denied` (D3).
4. **API scope** — Web and API policies aligned (Option B); see [ADMIN_RBAC_API_GAPS.md](./ADMIN_RBAC_API_GAPS.md).
5. **Login landing** — Role-specific homes via `GetDefaultHome()` (D2 / Phase 7.1).

---

## 4. Target access matrix (v1)

| Role (claim) | Display name | Allowed sections |
|--------------|--------------|------------------|
| `admin` | Admin | All: Overview, Orders, Customers, Products, Content, Affiliates, Support, Careers, System |
| `operations_manager` | Operations Manager | Overview, Orders, Affiliates, Careers |
| `support_team` | Customer Support | Overview, Support (chat / tickets / KB) |
| `merchandising` | Merchandising | Overview, Products |
| `content_team` | Creative & Content | Overview, Content |
| `technology_systems` | Technology & Systems | Overview, System |

**Default for non-admin (unless D1 overridden):** Customers → **admin only**.

### Demo accounts (admin login page)

| Email | Password | Role (profile) | Display name |
|-------|----------|----------------|--------------|
| admin@muuqwear.com | admin123 | admin | Admin |
| ops@muuqwear.com | ops123 | operations_manager | Operations Manager |
| support@muuqwear.com | support123 | support_team | Customer Support |
| merch@muuqwear.com | merch123 | merchandising | Merchandising |
| creative@muuqwear.com | creative123 | content_team | Creative & Content |
| tech@muuqwear.com | tech123 | technology_systems | Technology & Systems |

---

## 5. Route → section mapping

| Route(s) | Section key | Allowed roles (v1) |
|----------|-------------|-------------------|
| `/admin` | `overview` | All staff roles |
| `/admin/orders` | `orders` | admin, operations_manager |
| `/admin/customers` | `customers` | admin *(D1)* |
| `/admin/products` | `products` | admin, merchandising |
| `/admin/content` | `content` | admin, content_team |
| `/admin/affiliates` | `affiliates` | admin, operations_manager |
| `/admin/support`, `/admin/live-chat`, `/admin/tickets` | `support` | admin, support_team |
| `/admin/careers`, `/admin/jobs`, `/admin/jobs/{id}/applications` | `careers` | admin, operations_manager |
| `/admin/system` | `system` | admin, technology_systems |

---

## 6. Implementation phases

### Phase 0 — Backend / auth prerequisites

**Web and API aligned (Option B complete).** See [ADMIN_RBAC_API_GAPS.md](./ADMIN_RBAC_API_GAPS.md) and API repo `docs/API_RBAC.md`.

#### Step 0.1 — Inventory API role reality

- [x] Confirm `api/Auth/login` returns the role strings above for each demo user *(API / manual after re-login)*
- [x] Confirm cookie stores role as `ClaimTypes.Role` (`CookieAuthHelper`)
- [x] Confirm storefront login still rejects non-`user` (`LoginComponent.razor`)

#### Step 0.2 — Seed / verify demo users in API

- [x] Ensure all 6 demo users exist with correct passwords and roles (see §4) — `docs/scripts/seed-demo-admin-users.ps1`

#### Step 0.3 — API authorization audit

- [x] List admin endpoints and section policies *(API team — `docs/API_RBAC.md`)*
- [x] **Option B** implemented — policies match Web access matrix
- [x] Document alignment → [ADMIN_RBAC_API_GAPS.md](./ADMIN_RBAC_API_GAPS.md)

**Exit criteria:** Log in as each demo user; correct role claim in cookie / `LoggedInUserModel`.

---

### Phase 1 — Shared RBAC foundation (Web)

#### Step 1.1 — Expand `AdminPortalRoles` into an access map

- [x] Role constants + display names
- [x] Section keys: `overview`, `orders`, `customers`, `products`, `content`, `affiliates`, `support`, `careers`, `system`
- [x] Helpers: `CanAccess(role, section)`, `GetSections(role)`, `GetDisplayName(role)`, `GetDefaultHome(role)`
- [x] Single source of truth for login, nav, dashboard, `[Authorize]`, notifications

#### Step 1.2 — Map routes → sections

- [x] Use table in §5; centralize in one helper (avoid scattered string checks)

#### Step 1.3 — Authorization policies

- [x] Register ASP.NET policies per section (e.g. `AdminSection:Orders`)
- [x] Replace `[Authorize(Roles = AdminPortalRoles.All)]` with section policies on each page

#### Step 1.4 — Access-denied UX

- [x] Add `/admin/access-denied` (or chosen D3 behavior)
- [x] Wire `OnRedirectToAccessDenied` in `Program.cs` if appropriate

**Exit criteria:** One helper answers “can this role open this section?” everywhere.

---

### Phase 2 — Login & session

#### Step 2.1 — Admin login gate

- [x] Keep allow-list of 6 staff roles (`AdminPortalRoles.IsAllowed`)
- [x] Redirect to role home after success (`AdminPortalRoles.GetDefaultHome`)

#### Step 2.2 — Show real role in chrome

- [x] Sidebar: replace hardcoded `Admin` with role display name (`AdminNavMenuComponent.razor`)
- [x] Top bar user menu: show display name + role if applicable

#### Step 2.3 — Demo accounts table

- [x] Keep click-to-fill on admin login
- [x] Do not treat table as authorization (UX only)

**Exit criteria:** Login as ops → sidebar shows “Operations Manager”, not “Admin”.

---

### Phase 3 — Route enforcement (security boundary)

#### Step 3.1 — Per-page `[Authorize]` / policies

- [x] Apply section policies per §5 on all admin page components:
  - `AdminDashboardComponent.razor`
  - `AdminOrdersComponent.razor`
  - `AdminCustomerComponent.razor`
  - `AdminProductComponent.razor`
  - `AdminContentComponent.razor`
  - `AdminAffiliatesComponent.razor`
  - `AdminCustomerSupportComponent.razor`
  - `AdminCareerManagementComponent.razor`
  - `AdminApplicationsComponent.razor`
  - `AdminSystemTechnologyComponent.razor`

#### Step 3.2 — Deep links

- [x] Direct URL to forbidden section → access denied (not full access)

#### Step 3.3 — Related storefront checks

- [x] Review `ShippingReturnComponent.razor` (`IsInRole("admin")` only)
- [x] Decide if returns/refunds follow Orders (ops + admin) — all staff portal roles use admin orders; storefront return form hidden for staff

**Exit criteria:** Support user navigating to `/admin/products` is blocked.

---

### Phase 4 — Navigation filtering (UX boundary)

#### Step 4.1 — Filter `AdminNavMenuComponent` by role

- [x] Hide nav groups user cannot access
- [x] Keep Overview for all staff roles

#### Step 4.2 — Badge counts

- [x] Only fetch/show badges for allowed sections (orders, low stock, tickets, affiliates, etc.)

#### Step 4.3 — Mobile drawer

- [x] Same filtering as desktop (shared component)

**Exit criteria:** Merch user sees Overview + Products only.

---

### Phase 5 — Dashboard / Overview role awareness

#### Step 5.1 — Welcome copy

- [x] Role-specific subtitle (not “full access” for everyone)

#### Step 5.2 — “Your Access” list

- [x] Drive from access map, not hardcoded 8 modules (`BuildAccessModules`)

#### Step 5.3 — Stats & quick actions

- [x] Filter KPIs and quick actions by role:
  - Support → tickets
  - Merch → low stock
  - Ops → orders / affiliates / careers
  - Tech → system health
  - Admin → all

#### Step 5.4 — Recent activity

- [x] Filter activity types by allowed sections

**Exit criteria:** Ops overview shows orders/affiliates/careers access only.

---

### Phase 6 — Header notifications & messages (role-aware)

#### Step 6.1 — Filter notification types by role

- [x] Pending orders → ops + admin
- [x] Affiliate applications → ops + admin
- [x] Customer notes → support + admin *(and customers if D1 expands)*
- [x] Low stock → merch + admin

#### Step 6.2 — Messages bell

- [x] Same rules as customer notes

#### Step 6.3 — “View all” links

- [x] Only link to allowed destinations

**Files:** `AdminHeaderNotificationFeedBuilder.cs`, `AdminHeaderOperationalNotificationsBuilder.cs`, `AdminTopBarComponent.razor`

**Exit criteria:** Merch never sees pending-order notifications.

---

### Phase 7 — Cross-cutting polish

#### Step 7.1 — Default landing after login

- [x] Role home routes via `GetDefaultHome()` (Support → `/admin/support`, Merch → `/admin/products`, etc.)

#### Step 7.2 — API 403 handling

- [x] Friendly “no permission” messaging (`ApiErrorMessageHelper` for HTTP 403; `AdminUiErrorHelper.FromApi` normalizes forbidden text)

#### Step 7.3 — Documentation

- [x] Update `ADMIN_REDESIGN.md` §4 when RBAC is implemented
- [x] Record final D1 Customers decision (admin-only page; support gets header notes only)

#### Step 7.4 — Demo credentials

- [x] Dev-gate demo password table (`IWebHostEnvironment.IsDevelopment()` on admin login)

---

### Phase 8 — Verification matrix (manual QA)

**Run before production release.** Automated matrix tests: `dotnet test MuuqWear.Tests --filter AdminPortalRbac`.

For **each** of the 6 demo accounts:

| Check | admin | ops | support | merch | creative | tech |
|-------|-------|-----|---------|-------|----------|------|
| Login succeeds | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Sidebar shows correct role label | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Only allowed nav items visible | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Allowed pages load data | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Forbidden URLs denied | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Overview “Your Access” matches role | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Notifications match role | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Logout → login as another role (no stale state) | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |

**Also verify:**

- [ ] Admin sees everything
- [ ] Storefront login rejects staff roles
- [ ] Admin login rejects `user` role
- [ ] API calls succeed for allowed sections (run `docs/scripts/rbac-smoke-test.ps1` + manual pass per role)
- [ ] Re-login after API deploy so JWT carries `app_role`

---

## 7. Recommended implementation order

```text
0  Backend role verification / seeding
1  Access map + helpers + policies
2  Route [Authorize] per section
3  Nav filtering + role label
4  Dashboard filtering
5  Notifications filtering
6  Access-denied UX + docs + test pass
7  API policy alignment (if not done in Phase 0)
```

Phases **1–5** can ship in Blazor alone.  
Phases **0 + 7 (API)** are required for real security.

---

## 8. Rough effort

| Phase | Relative size | Depends on |
|-------|---------------|------------|
| 0 Backend | Medium / external | API access |
| 1 Foundation | Small–medium | D1–D6 locked |
| 2 Login chrome | Small | Phase 1 |
| 3 Route guards | Medium | Phase 1 |
| 4 Nav | Medium | Phase 1 |
| 5 Dashboard | Medium | Phase 1 |
| 6 Notifications | Medium | Phase 1 |
| 7 Polish + docs | Small | Phases 2–6 |
| 8 QA | Medium | All |

---

## 9. Explicitly out of v1

- Fine-grained permissions inside a page (e.g. “can refund but not cancel”)
- Multi-role users (one user, multiple roles)
- Admin user invite UI wired to `IAdminSettingService` (future phase)
- Replacing cookie auth / JWT claim redesign

---

## 10. Agent handoff notes

When resuming RBAC work:

1. Read this file first; confirm open questions in §3 are answered
2. Do **Phase 0** before claiming RBAC is “done”
3. Implement **Phase 1** (`AdminPortalRoles` access map) before touching nav/dashboard/notifications
4. Do not duplicate role strings across components — use shared helpers only
5. After implementation, check off boxes in §6 Phase 8 (manual QA) and run `dotnet test` for RBAC unit tests
