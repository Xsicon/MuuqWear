# API Handoff: Dynamic Affiliate Tier Settings

**Target repo:** `MuuqWearApi`  
**Consumer (after API is done):** `MuuqWear` → `/admin/affiliates?tab=tiers` (`AdminAffiliatesComponent`)  
**Design reference:** `.design-reference/backend/AffiliateProgram.tsx` (Tier Settings tab)  
**Schema:** `"MuuqWear"` (Supabase — same as existing affiliate tables)

---

## Prompt (copy from here)

Implement **dynamic affiliate tier configuration** in MuuqWearApi so the Blazor admin **Affiliates → Tier Settings** tab can load and save tier rules from the database instead of hardcoded values.

### Background

Today tier logic is **hardcoded** in `AffiliateService.GetCommissionRate()`:

```csharp
// MuuqWearApi/MuuqWear.Application/Service/AffiliateService.cs ~748
profile.AffiliateTier?.ToLower() switch
{
    "bronze" => 5,
    "silver" => 10,
    "gold" => 15,
    _ => 5
};
```

New affiliates are always approved to `"bronze"` (`ApproveApplication`, `UpdateApplicationStatus`).

The storefront **Milestones** page (`MuuqWear.Web/.../MileStoneComponent.razor`) shows **different** marketing copy (2.5% / 5% / 10% commission, 50/150/500 items, referral discounts). **Pick one canonical source** — seed defaults should match `GetCommissionRate` behavior unless product decides to align Milestones UI in a follow-up.

Admin UI is already built but **read-only** with placeholder cards for Bronze / Silver / Gold (commission rate + items sold threshold).

---

### Goals (Phase 1 — required for Blazor wiring)

1. **Persist tier config** in Supabase (`MuuqWear` schema).
2. **Admin API** to list and update tiers (`[Authorize(Roles = "admin")]`).
3. **Refactor** `GetCommissionRate()` (and any other tier lookups) to read from DB, with safe fallback if row missing.
4. **SQL migration + seed** with the three default tiers (idempotent).
5. **DTOs + tests** for validation rules.

### Non-goals (Phase 2 — optional, document if deferred)

- Admin **Payouts** tab (`Process Payout` on `affiliate_referrals`)
- **Auto tier promotion** when `profiles.affiliate_items_sold` crosses threshold (batch job or on referral track)
- **Recalculate commissions** tool (design reference `SystemTechnology.tsx`)
- Milestone **cash bonuses** (50/100/250/500/1000 items) — still client-calculated in `MileStoneComponent` today

---

### Proposed database table

```sql
CREATE TABLE IF NOT EXISTS "MuuqWear".affiliate_tiers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    slug text NOT NULL UNIQUE,              -- 'bronze' | 'silver' | 'gold'
    display_name text NOT NULL,             -- 'Bronze'
    items_sold_threshold int NOT NULL,      -- min items to qualify for this tier
    commission_rate_percent numeric(5,2) NOT NULL,  -- e.g. 5.00
    referral_discount_percent numeric(5,2) NOT NULL DEFAULT 0,  -- partner store discount
    sort_order int NOT NULL DEFAULT 0,      -- bronze=1, silver=2, gold=3
    is_active boolean NOT NULL DEFAULT true,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by uuid NULL REFERENCES auth.users(id)
);

CREATE INDEX IF NOT EXISTS idx_affiliate_tiers_sort
    ON "MuuqWear".affiliate_tiers (sort_order);
```

**Seed defaults (match current `GetCommissionRate` unless product says otherwise):**

| slug   | display_name | items_sold_threshold | commission_rate_percent | referral_discount_percent | sort_order |
|--------|--------------|----------------------|-------------------------|---------------------------|------------|
| bronze | Bronze       | 0                    | 5.00                    | 5.00                      | 1          |
| silver | Silver       | 150                  | 10.00                   | 10.00                     | 2          |
| gold   | Gold         | 500                  | 15.00                   | 15.00                     | 3          |

Add `docs/scripts/seed-affiliate-tiers.sql` (or extend affiliate seed) with fixed UUIDs + `ON CONFLICT (slug) DO UPDATE`.

---

### API endpoints

Add to `AffiliateController` (or new `AffiliateTierController` under `api/Affiliate/admin/...`):

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/api/Affiliate/admin/tiers` | admin | List all tiers ordered by `sort_order` |
| `GET` | `/api/Affiliate/admin/tiers/{slug}` | admin | Single tier by slug |
| `PUT` | `/api/Affiliate/admin/tiers/{slug}` | admin | Update tier fields (partial body OK) |

**Optional (Phase 1.5 — enables public Milestones page to stay in sync):**

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/api/Affiliate/tiers` | anonymous or authenticated | Read-only active tiers for storefront |

---

### DTOs (suggested)

```csharp
// MuuqWear.Model/DTO/AffiliateApplicationDTO/AffiliateTierDTO.cs
public class AffiliateTierDTO
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;           // bronze
    public string DisplayName { get; set; } = string.Empty;    // Bronze
    public int ItemsSoldThreshold { get; set; }
    public decimal CommissionRatePercent { get; set; }
    public decimal ReferralDiscountPercent { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateAffiliateTierDTO
{
    public string? DisplayName { get; set; }
    public int? ItemsSoldThreshold { get; set; }
    public decimal? CommissionRatePercent { get; set; }
    public decimal? ReferralDiscountPercent { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}
```

Wrap in existing `Response<T>` pattern.

---

### Validation rules

- `slug` is immutable after create (only update via PUT on existing slug).
- `commission_rate_percent`: 0–100
- `referral_discount_percent`: 0–100
- `items_sold_threshold`: ≥ 0, **strictly increasing** across active tiers by `sort_order` (bronze < silver < gold)
- Cannot deactivate **bronze** if it is the default approval tier (or require at least one active tier)
- At least one tier must remain active

---

### Service changes

1. **`IAffiliateService` / `AffiliateService`**
   - `GetAdminTiersAsync()`
   - `UpdateAdminTierAsync(string slug, UpdateAffiliateTierDTO, Guid adminUserId)`
   - `GetCommissionRate(string affiliateCode)` → load tier from `profiles.affiliate_tier`, join `affiliate_tiers`, return `commission_rate_percent`
   - Cache tiers in memory for 5 minutes (optional) to avoid DB hit on every order referral

2. **`TrackOrderReferral`** — no change to signature; it already calls `GetCommissionRate`.

3. **`ApproveApplication`** — still sets `profiles.affiliate_tier = 'bronze'`; verify bronze tier exists in DB.

4. **Partner store discount** — if `referral_discount_percent` is stored per tier, consider using it in partner store pricing later (out of scope unless trivial).

---

### Files to touch (MuuqWearApi)

| Area | Files |
|------|--------|
| Model | `MuuqWear.Model/Models/AffiliateApplication/AffiliateTier.cs` |
| DTO | `AffiliateTierDTO.cs`, `UpdateAffiliateTierDTO.cs` |
| Service | `IAffiliateService.cs`, `AffiliateService.cs` |
| Controller | `AffiliateController.cs` |
| SQL | `docs/scripts/create-affiliate-tiers.sql`, `docs/scripts/seed-affiliate-tiers.sql` |
| Tests | Unit tests for validation + `GetCommissionRate` with mocked tier rows |

---

### Acceptance criteria

- [ ] `GET /api/Affiliate/admin/tiers` returns 3 tiers after seed
- [ ] `PUT /api/Affiliate/admin/tiers/gold` updates commission rate; subsequent `GET test/commission-rate/{code}` for a gold affiliate reflects new rate
- [ ] Invalid threshold order returns 400 with clear message
- [ ] Non-admin receives 403 on admin tier routes
- [ ] Migration + seed are idempotent
- [ ] Existing affiliate flows (apply, approve, track referral) still work without regression

---

### What Blazor will do after API is ready (MuuqWear — separate task)

1. Add models + `IAffiliateService` methods in `MuuqWear.Application`:
   - `GetAdminTiersAsync()`
   - `UpdateAdminTierAsync(string slug, UpdateAffiliateTierModel request)`
2. Replace read-only tier cards in `AdminAffiliatesComponent.razor` (`tab=tiers`) with editable form per tier + Save.
3. Remove readonly banner; show live values from API.
4. Optional: update `MileStoneComponent` to call public `GET /api/Affiliate/tiers` instead of hardcoded tier benefits.

**Notify frontend when these endpoints are deployed and sample response JSON is stable.**

---

### Example responses

**GET `/api/Affiliate/admin/tiers`**

```json
{
  "success": true,
  "message": "Tiers fetched",
  "data": [
    {
      "id": "t0000001-0000-4000-8000-000000000001",
      "slug": "bronze",
      "displayName": "Bronze",
      "itemsSoldThreshold": 0,
      "commissionRatePercent": 5.0,
      "referralDiscountPercent": 5.0,
      "sortOrder": 1,
      "isActive": true,
      "updatedAt": "2026-07-13T08:00:00Z"
    }
  ]
}
```

**PUT `/api/Affiliate/admin/tiers/silver`**

```json
{
  "commissionRatePercent": 12.0,
  "itemsSoldThreshold": 200
}
```

---

### Related existing tables (do not break)

- `"MuuqWear".profiles` — `affiliate_tier` (string slug), `affiliate_items_sold`, `affiliate_commission_earned`
- `"MuuqWear".affiliate_applications`
- `"MuuqWear".affiliate_referrals` — stores `commission_rate` snapshot at referral time

---

### Open question for product/API owner

**Commission rates:** Align with `AffiliateService` (5/10/15%) or Milestones UI copy (2.5/5/10%)? Default seed should match chosen source; document decision in API PR description.

---

## End of prompt
