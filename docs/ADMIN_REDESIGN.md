# Muuqwear Admin Redesign — Reference & Implementation Plan

> **Branch:** `feature/admin` (from latest `main`)  
> **Scope:** Redesign the internal dashboard at `/admin` to match the Figma + React prototype.  
> **Approach:** One section at a time. Keep existing Blazor routes/services; update UI/UX only unless a gap requires backend work.

---

## 1. What this design is

This is the **Muuqwear Internal Dashboard** — the admin/backend area served at `http://localhost:5276/admin`.

It is **not a single page**. It is a **shell** (sidebar + top bar) plus **multiple admin sections** for operating the store: orders, customers, products, content, affiliates, support, analytics, careers, and system settings.

### Design sources (two references)

| Source | Location | Notes |
|--------|----------|-------|
| **Figma export (`backend.zip`)** | `.design-reference/backend/*.tsx` | 9 section components; role-based overview; rounded cards; pill badges |
| **React prototype (`AdminDashboard`)** | User-provided monolith component | Single SPA with 10 nav keys; analytics charts (Recharts); notification dropdown |

**Implementation target:** Blazor components under `MuuqWear.Web/Components/Pages/AdminComponent/` + shared layout under `MuuqWear.Web/Components/Layout/AdminLayoutComponent/`.

**Keep:** Separate routes per section (current Blazor pattern). Do **not** convert to a single-page tab switcher unless explicitly requested.

---

## 2. Design tokens (use across all sections)

### Colors
| Token | Hex | Usage |
|-------|-----|-------|
| Navy | `#1E2A47` | Primary buttons, active nav, headings |
| Navy dark | `#0B1A33` | Primary hover |
| Slate | `#4A5C7A` | Secondary text, labels |
| Border | `#A9B7CC` / `#E5E7EB` | Card borders, inputs |
| Page bg | `#F4F6F9` | Main content background |
| Card bg | `#FFFFFF` | Cards, sidebar, top bar |
| Accent bg | `#E6ECF5` | Icon wells, inactive filters |
| Success | `#2D9C5A` / `#D1FAE5` | Connected, in stock, approved |
| Warning | `#ED6C02` / `#FFF3CD` / `#FEF3C7` | Pending, low stock |
| Danger | `#C44545` / `#FEE2E2` | Deny, out of stock, errors |
| Info | `#CCE5FF` / `#DBEAFE` | Processing, shipped |

### Typography
- **Font:** Manrope (already used on storefront)
- **Page title:** 21–28px, weight 800, tracking -0.5px
- **Section title:** 14px uppercase, weight 700, tracking 0.5px
- **Card title:** 13–14px, weight 700
- **Body:** 12–13px
- **Labels:** 11px uppercase, tracking 1px, weight 700

### Components (shared patterns to build once)
- `SectionCard` — white card, border, header row with title + actions
- `StatusBadge` — uppercase pill by status (Pending, Shipped, Published, etc.)
- `Btn` variants — primary (navy), secondary (white border), ghost, danger
- Filter chips — pill toggles; active = navy bg + white text
- Data table — uppercase headers, row hover `#F4F6F9`
- Stat card — label + large value + trend arrow

### Layout shell
- **Sidebar:** 260px fixed, white, border-right; logo + user block + nav
- **Top bar:** sticky, search, bell (notifications), mail, profile
- **Mobile:** sidebar drawer + overlay
- **Content padding:** 16px mobile / 21px desktop

---

## 3. Navigation map

### Figma `backend/` files → Blazor routes

| # | Figma component | Proposed nav label | Blazor route | Blazor component | Status |
|---|-----------------|-------------------|--------------|------------------|--------|
| 0 | *(shell)* | — | — | `AdminDashboardLayoutComponent`, `AdminNavMenuComponent`, `AdminTopBarComponent` | Exists — restyle |
| 1 | `Overview.tsx` | Overview | `/admin` | `AdminDashboardComponent.razor` | Exists — redesign |
| 2 | `SalesOrders.tsx` | Sales & Orders | `/admin/orders` | `AdminOrdersComponent.razor` | Exists — redesign |
| 3 | `Customers.tsx` | Customers | `/admin/customers` | `AdminCustomerComponent.razor` | Exists — redesign |
| 4 | `ProductsInventory.tsx` | Products & Inventory | `/admin/products` | `AdminProductComponent.razor` | Exists — redesign |
| 5 | `Content.tsx` | Content | `/admin/content` | `AdminContentComponent.razor` | Exists — redesign |
| 6 | `AffiliateProgram.tsx` | Affiliates | `/admin/affiliates` | `AdminAffiliatesComponent.razor` | Exists — redesign |
| 7 | `CustomerSupport.tsx` | Customer Support | `/admin/live-chat`, `/admin/tickets` | `AdminLiveChatComponent`, `AdminTicketComponent` | Exists — redesign |
| 8 | `CareerManagement.tsx` | Careers | `/admin/jobs` | `AdminJobsComponent.razor` | Exists — redesign |
| 9 | `SystemTechnology.tsx` | System & Technology | `/admin/settings` *(partial)* | `AdminSettingsComponent.razor` | Exists — split/extend |
| 10 | *(React only)* | Analytics | `/admin/analytics` | `AdminAnalyticsComponent.razor` | Exists — redesign |

### Figma vs current nav differences
- Figma **groups** live chat + tickets + knowledge base under **Customer Support** (tabs).
- Figma has **Career Management** as its own top-level area (matches current `/admin/jobs`).
- Figma **System & Technology** (health, integrations, sync, logs, background jobs) overlaps with **Settings** in current app — decide during Settings phase whether to add `/admin/system` or use Settings tabs.
- React prototype adds **Analytics** as separate nav (already exists in Blazor).

---

## 4. Role-based access (from Figma `Overview.tsx`)

Figma defines dashboard roles and what each role sees:

| Role | Sections |
|------|----------|
| **Admin** | All 8 areas |
| **Operations Manager** | Sales & Orders, Affiliates, Career Management |
| **Customer Support** | Customer Support (chat + tickets) |
| **Merchandising** | Products & Inventory |
| **Creative & Content** | Content |
| **Technology & Systems** | System & Technology |

**Note:** Current Blazor uses `[Authorize(Roles = "admin")]` on most pages. Role-based nav filtering is a **future enhancement** — document but defer unless backend roles already exist.

---

## 5. Section specs & mock data

### 5.1 Overview (`Overview.tsx` + React `OverviewSection`)
**Route:** `/admin` → `AdminDashboardComponent.razor`

**Figma version includes:**
- Welcome header: `Welcome back, {name}!` + role message
- Stats grid (4): Active Orders, Pending Support, Low Stock Items, System Status
- Two columns: **Your Access** (role modules) + **Recent Activity**
- **Quick Actions:** View All Orders, Export Data, System Health

**React monolith version includes:**
- KPI cards: Revenue, Orders, New Affiliates, Avg Rating (with % change)
- Recent Orders list + Affiliate Applications preview
- System Health strip (Database, Stripe, Supabase, Backup, Active Users)

**Current Blazor:** Already loads real order revenue/count via `IOrderService`; affiliate count partially mocked.

**Merge plan:** Combine real KPIs from services with Figma welcome/activity/access blocks. Wire Recent Activity from notifications or order events later.

---

### 5.2 Sales & Orders (`SalesOrders.tsx`)
**Route:** `/admin/orders` → `AdminOrdersComponent.razor`

**Tabs:** Orders | Returns | Refunds *(Figma adds Refunds tab vs current Orders + Returns only)*

**Order fields:** `id`, `customer`, `date`, `total`, `status`, `items`  
**Statuses:** All, Pending, Processing, Shipped, Delivered  
**Actions:** Export, View (eye), Process  
**Return fields:** `id`, `order`, `customer`, `date`, `reason`, `status` — Approve / Deny  
**Refund fields:** `id`, `order`, `customer`, `amount`, `date`, `status` — Process Refund

**Services:** `IOrderService`, existing orders/returns logic in `AdminOrdersComponent.razor.cs`

---

### 5.3 Customers (`Customers.tsx`)
**Route:** `/admin/customers` → `AdminCustomerComponent.razor`

**Sidebar (expandable, per design ref):**
- Customer List → `?view=list` (default)
- Customer Details → `?view=details` (profile + threaded internal notes)
- Customer Notes → `?view=notes` (table of note summaries + panel)
- Badge: total customer count from `IAdminBadgeService`

**Internal notes (threaded, admin-only):**
- `GET api/Customer/{customerId}/notes` — list notes (newest first)
- `POST api/Customer/{customerId}/notes` — add note; author from JWT
- Extend `GET api/Customer` rows with `NoteCount`, `LatestNotePreview`, `LatestNoteAt`, `LatestNoteAuthorName`, `LatestNoteAuthorRole`
- Each note must include `AuthorName` and optional `AuthorRole` (displayed as `[Sarah - Support]`)

**Table columns (Customer List):** Name, Email, Orders, Total Spent, Joined, Last Order, Actions (view eye)  
**Header:** `CUSTOMERS (n)` + Export button

**Mock row shape:**
```ts
{ name, email, orders, spent, joined, lastOrder }
```

---

### 5.4 Products & Inventory (`ProductsInventory.tsx`)
**Route:** `/admin/products` → `AdminProductComponent.razor`

**Header actions:** Add New Product, Import CSV, Export  
**Filters:** All, Clothing, Accessories, Low Stock, Out of Stock  
**Product card fields:** `name`, `sku`, `price`, `category`, `sizes` (record), `status`  
**Size chips:** green ≥5, yellow 1–4, red 0  
**Statuses:** In Stock, Low Stock, Out of Stock  
**Actions:** Edit, Update Stock / Restock  
**Alert banner:** N products below threshold

**Services:** `IProductService`, existing product CRUD in `AdminProductComponent.razor.cs`

---

### 5.5 Content (`Content.tsx`)
**Route:** `/admin/content` → `AdminContentComponent.razor`

**Tabs:** Journal Articles | Events | Design History  
**Article fields:** `title`, `status` (Published/Draft), `date`, `views`  
**Actions:** Edit, View, Publish/Unpublish, Delete  
**Events tab (React monolith):** Muuqsimo 2025 — date, status, tickets sold, revenue  
**Design History tab:** recent add/update entries

**Services:** Existing content categories in `AdminContentComponent.razor.cs`

---

### 5.6 Affiliates (`AffiliateProgram.tsx`)
**Route:** `/admin/affiliates` → `AdminAffiliatesComponent.razor`

**Tabs:** Pending Applications | Active Affiliates | Payouts | Tier Settings *(Figma)*

**Pending application fields:** `name`, `handle`, `followers`, `date`, `niche` + content sample links  
**Actions:** Approve, Deny, Waitlist, View Full Application  
**Active affiliate fields:** `name`, `tier` (Gold/Silver/Bronze), `items`, `earned`, `lastSale`, `status`  
**Payout fields:** `name`, `amount`, `date`, `status` — Process Payout  
**Filters:** All Niches, All Tiers

**Services:** `AdminAffiliatesComponent.razor.cs`, `AffiliateRowComponent`

---

### 5.7 Customer Support (`CustomerSupport.tsx`)
**Routes:** `/admin/live-chat`, `/admin/tickets`

**Tabs:** Live Chat | Support Tickets | Knowledge Base *(Figma)*

**Live chat:** 1/3 conversation list + 2/3 thread; fields `name`, `waiting`, `order`, `status` (waiting/active)  
**Tickets table:** Ticket ID, Priority, Subject, Customer, Category, Date, Status  
**Knowledge base:** Help articles / FAQs *(may map to HelpCenter admin — verify)*

**Services:** Ticket/help services; `AdminTicketComponent`, `AdminLiveChatComponent`

---

### 5.8 Careers (`CareerManagement.tsx`)
**Route:** `/admin/jobs` → `AdminJobsComponent.razor`  
**Sub-route:** `/admin/jobs/{JobId}/applications` → `AdminApplicationsComponent.razor`

**Tabs:** Job Postings | Applications Inbox | Career Page Settings

**Job posting fields:** `title`, `department`, `location`, `status`, `applications`  
**Actions:** Edit, Duplicate, Close, View Applications  
**Application fields:** `status` (New/Reviewed/Interview), `name`, `position`, `date`, `portfolio`, `experience`  
**Actions:** View Full Application, Mark Reviewed, Interview, Reject  
**Settings:** Hero text, culture description

---

### 5.9 System & Technology (`SystemTechnology.tsx`)
**Route:** `/admin/settings` (partial) or new `/admin/system`

**Tabs:** System Health | Integrations | Sync Tools | Logs | Background Jobs

**Health cards:** Database, Stripe, Supabase — Connected + detail text  
**Integrations:** Stripe, Supabase, SendGrid, Cloudflare — Reconnect, Test  
**Sync tools:** Resync Stripe Orders, Recalculate Commissions, Sync Inventory, Clear Cache  
**Logs:** timestamp, level (Error/Warning/Info), message + filters  
**Background jobs:** placeholder in Figma

**Services:** `IAdminSettingService`, `AdminSettingsComponent.razor.cs`

---

### 5.10 Analytics *(React monolith only)*
**Route:** `/admin/analytics` → `AdminAnalyticsComponent.razor`

**Widgets:** Revenue area chart (30 days), Top Products, Affiliate tier performance, Sales by category pie chart, Customer insights  
**Note:** May need chart library for Blazor (e.g. existing approach or JS interop).

---

### 5.11 Settings *(React monolith — Users tab)*
**Route:** `/admin/settings` → `AdminSettingsComponent.razor`

**Tabs:** Users & Roles | Integrations | System  
**User fields:** `name`, `email`, `role`, `lastActive`  
**System fields:** Site Name, Contact Email, Currency, Tax Rate, Free Shipping Threshold

---

## 6. Shared Blazor files to touch (by phase)

| File | Purpose |
|------|---------|
| `AdminDashboardLayoutComponent.razor` + `.css` | Page shell |
| `AdminNavMenuComponent.razor` + `.css` | Sidebar nav |
| `AdminTopBarComponent.razor` + `.css` | Search, notifications, profile |
| `wwwroot/app.css` or new `admin.css` | Shared admin tokens (optional) |
| Per-section `Admin*Component.razor` + `.css` | Section content |

**Design reference assets:** `.design-reference/backend/` (extracted from `backend.zip` — do not ship to production).

---

## 7. Implementation order (one section at a time)

| Phase | Section | Why this order |
|-------|---------|----------------|
| **0** | **Admin shell** (layout, sidebar, top bar) | Foundation for every page; establishes tokens |
| **1** | **Overview** `/admin` | Landing page; stat cards + activity set patterns |
| **2** | **Sales & Orders** `/admin/orders` | Core ops; high traffic |
| **3** | **Products & Inventory** `/admin/products` | Large existing component; catalog management |
| **4** | **Customers** `/admin/customers` | Simpler table section |
| **5** | **Content** `/admin/content` | Tabbed CMS pattern |
| **6** | **Affiliates** `/admin/affiliates` | Multi-tab workflows |
| **7** | **Customer Support** (chat + tickets) | Split views / tables |
| **8** | **Careers** `/admin/jobs` | Jobs + applications |
| **9** | **Analytics** `/admin/analytics` | Charts last (heavier) |
| **10** | **Settings + System** `/admin/settings` | Users, integrations, system config |

---

## 8. Per-section checklist (repeat for each phase)

- [ ] Read Figma `.tsx` + compare to current Blazor component
- [ ] **Check `.design-reference/backend/` and `BackendDashboard.tsx` for expandable sidebar sub-items** (Orders, Customers, Products, Content, etc.)
- [ ] Update markup to match new layout (cards, badges, filters)
- [ ] Update scoped CSS (match tokens in §2)
- [ ] Keep existing `@inject` services and `.razor.cs` logic
- [ ] Replace mock-only UI with real data where service exists
- [ ] Verify mobile (sidebar drawer, table scroll, stacked cards)
- [ ] Verify `[Authorize]` still applied
- [ ] Manual test at route on `localhost:5276`

---

## 9. Data & backend gaps to watch

| Feature | In design | In current app | Action |
|---------|-----------|----------------|--------|
| Refunds tab | Yes (Figma) | Verify orders component | Add tab if missing |
| Knowledge base admin | Yes (Figma) | Help center exists | Map or defer |
| Tier settings (affiliates) | Yes | Verify affiliates | Add UI if missing |
| Role-based nav | Yes (Figma) | Admin-only | Defer |
| Analytics charts | Yes (React) | Partial | Chart library decision |
| System logs / sync tools | Yes (Figma) | May be mock | Backend endpoints TBD |
| Notifications dropdown | Yes (React) | Badge counts exist | Wire to `IAdminBadgeService` |
| Cloudflare integration | Figma System | — | UI only / defer |

---

## 10. First section to implement

### → **Phase 0: Admin Shell** (layout + sidebar + top bar)

Start here before any individual page content. Every other section inherits sidebar width, top bar height, colors, and mobile behavior from this shell.

**Files:**
- `MuuqWear.Web/Components/Layout/AdminLayoutComponent/AdminDashboardLayoutComponent.razor(.css)`
- `MuuqWear.Web/Components/Layout/AdminLayoutComponent/AdminNavMenuComponent.razor(.css)`
- `MuuqWear.Web/Components/Layout/AdminLayoutComponent/AdminTopBarComponent.razor(.css)`

**Goals:**
- Match Figma shell: 260px sidebar, `#F4F6F9` page bg, white sidebar/top bar
- Nav labels/icons aligned with §3 table
- Notification bell with badge (use existing `IAdminBadgeService`)
- Mobile drawer behavior preserved

### → **Phase 1: Overview** (`/admin`) — first *content* section after shell

Once the shell is done, **Overview** is the first content section because it is the admin home page and defines stat cards, section cards, and activity lists reused elsewhere.

**File:** `AdminDashboardComponent.razor(.css)`

---

## 11. Reference file index

```
.design-reference/backend/
├── Overview.tsx           → /admin
├── SalesOrders.tsx        → /admin/orders
├── Customers.tsx          → /admin/customers
├── ProductsInventory.tsx  → /admin/products
├── Content.tsx            → /admin/content
├── AffiliateProgram.tsx   → /admin/affiliates
├── CustomerSupport.tsx    → /admin/live-chat, /admin/tickets
├── CareerManagement.tsx   → /admin/jobs
└── SystemTechnology.tsx   → /admin/settings (or /admin/system)
```

---

*Last updated: 2026-07-02 — created for `feature/admin` branch redesign work.*
