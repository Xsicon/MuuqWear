# API Handoff: Affiliate Admin Payouts

**Target repo:** `MuuqWearApi`  
**Consumer (after API is done):** `MuuqWear` → `/admin/affiliates?tab=payouts` (`AdminAffiliatesComponent`)  
**Design reference:** `.design-reference/backend/AffiliateProgram.tsx` (Payouts tab)  
**Schema:** `"MuuqWear"` (Supabase)  
**Related seed:** `MuuqWear/docs/scripts/seed-affiliate-program.sql` (4 pending `affiliate_referrals`)

---

## Prompt (copy from here)

Implement **admin affiliate payout processing** in MuuqWearApi so the Blazor admin **Affiliates → Payouts** tab can list pending commissions and mark them as paid.

### Background

Today commissions are **recorded but never paid out via admin**:

1. `TrackOrderReferral()` inserts `affiliate_referrals` with `status = 'pending'`.
2. Profile stats (`affiliate_commission_earned`, `affiliate_items_sold`) are updated immediately on referral track.
3. `GetAffiliateInfo()` already computes `CommissionPending` as sum of referrals where `status == 'pending'`.
4. **No admin payout endpoints exist.** The Blazor Payouts tab is a placeholder.

**Existing table** (`AffiliateReferral.cs`):

| Column | Type | Notes |
|--------|------|-------|
| `id` | uuid | PK |
| `order_id` | uuid | FK → orders |
| `affiliate_code` | text | |
| `user_id` | uuid | buyer user id |
| `order_total` | numeric | |
| `commission_amount` | numeric | |
| `commission_rate` | int | snapshot % |
| `status` | text | `'pending'` today; seed uses pending |
| `created_at` | timestamptz | |

**Design target** (Figma / `AffiliateProgram.tsx`):

- Tab title: **Payouts (N)** — N = count of pending payout rows
- Each row: affiliate **name**, **amount**, **date**, **status**
- Action: **Process Payout** (green button) per row
- Rows are **one per affiliate** with **aggregated pending balance**, not one row per referral

**MuuqWear admin placeholder:** `/admin/affiliates?tab=payouts` — readonly banner + empty state until this API ships.

---

### Goals (Phase 1 — required for Blazor wiring)

1. **List pending payouts** grouped by affiliate (name, tier, total amount, referral count, oldest pending date).
2. **Process payout** for an affiliate — marks all their `pending` referrals as paid in one atomic operation.
3. **Audit trail** — new `affiliate_payouts` batch table (who processed, when, how much).
4. **Admin badge count** — add pending payout count to `AdminBadgeCountsDTO` (optional but recommended).
5. **DTOs + validation** following existing `Response<T>` pattern.
6. **SQL migration** (idempotent) + extend seed if needed.

### Non-goals (Phase 2 — document if deferred)

- Affiliate **Request Payout** from profile (`ProfileComponent` — UI only, $50 minimum, PayPal/bank/store credit)
- External payment provider integration (PayPal API, Stripe Connect)
- Partial payout (pay subset of pending referrals)
- Cancel / dispute referral commissions
- Email notification on payout processed

---

### Proposed database changes

#### 1. New table: `affiliate_payouts` (batch audit)

```sql
CREATE TABLE IF NOT EXISTS "MuuqWear".affiliate_payouts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    affiliate_code text NOT NULL,
    profile_id uuid NOT NULL REFERENCES "MuuqWear".profiles(id),
    total_amount numeric(12,2) NOT NULL CHECK (total_amount > 0),
    referral_count int NOT NULL CHECK (referral_count > 0),
    status text NOT NULL DEFAULT 'completed'
        CHECK (status IN ('completed', 'cancelled')),
    payment_method text NULL,   -- 'manual' default; future: paypal, bank_transfer, store_credit
    admin_notes text NULL,
    processed_at timestamptz NOT NULL DEFAULT now(),
    processed_by uuid NOT NULL REFERENCES auth.users(id)
);

CREATE INDEX IF NOT EXISTS idx_affiliate_payouts_code
    ON "MuuqWear".affiliate_payouts (affiliate_code);

CREATE INDEX IF NOT EXISTS idx_affiliate_payouts_processed_at
    ON "MuuqWear".affiliate_payouts (processed_at DESC);
```

#### 2. Extend `affiliate_referrals`

```sql
ALTER TABLE "MuuqWear".affiliate_referrals
    ADD COLUMN IF NOT EXISTS paid_at timestamptz NULL,
    ADD COLUMN IF NOT EXISTS payout_id uuid NULL
        REFERENCES "MuuqWear".affiliate_payouts(id),
    ADD COLUMN IF NOT EXISTS processed_by uuid NULL
        REFERENCES auth.users(id);

CREATE INDEX IF NOT EXISTS idx_affiliate_referrals_status
    ON "MuuqWear".affiliate_referrals (status);

CREATE INDEX IF NOT EXISTS idx_affiliate_referrals_affiliate_code_status
    ON "MuuqWear".affiliate_referrals (affiliate_code, status);
```

#### Status vocabulary (canonical)

| Status | Meaning |
|--------|---------|
| `pending` | Commission earned, not yet paid out to affiliate |
| `paid` | Admin processed payout (was `pending`) |
| `cancelled` | Commission voided (future — fraud, refund) |

> `RecentReferralDTO` comments mention `completed` — use **`paid`** in DB/API for processed referrals; map to `completed` in user-facing copy if needed.

**Do not decrement** `profiles.affiliate_commission_earned` on payout — that field is lifetime earned. Pending balance = `SUM(commission_amount) WHERE status = 'pending'`.

---

### API endpoints

Add to `AffiliateController` under `api/Affiliate/admin/...`:

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/api/Affiliate/admin/payouts` | admin | Pending payouts **grouped by affiliate** (default). Query: `?status=pending` (default), `paid` for history groups optional |
| `GET` | `/api/Affiliate/admin/payouts/{affiliateCode}/referrals` | admin | Pending referral line items for one affiliate (expand/detail) |
| `POST` | `/api/Affiliate/admin/payouts/{affiliateCode}/process` | admin | Process all pending referrals for affiliate |
| `GET` | `/api/Affiliate/admin/payouts/history` | admin | Paginated processed payout batches (`affiliate_payouts`). Query: `page`, `pageSize` |

**Optional (Phase 1.5):**

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/api/Affiliate/admin/payouts/summary` | admin | `{ pendingAffiliateCount, pendingTotalAmount, processedThisMonth }` |

---

### DTOs (suggested)

```csharp
// MuuqWear.Model/DTO/AffiliateApplicationDTO/AffiliatePendingPayoutDTO.cs
public class AffiliatePendingPayoutDTO
{
    public string AffiliateCode { get; set; } = string.Empty;
    public string AffiliateName { get; set; } = string.Empty;      // profiles.full_name
    public string AffiliateTier { get; set; } = string.Empty;      // bronze | silver | gold
    public decimal TotalAmount { get; set; }                       // sum pending commissions
    public int ReferralCount { get; set; }
    public DateTime OldestPendingDate { get; set; }                // min(created_at) — display date on card
    public string Status { get; set; } = "pending";                // always pending on list endpoint
    public string FormattedDate => OldestPendingDate.ToString("MMM dd, yyyy");
    public string FormattedAmount => $"${TotalAmount:F2}";
}

// Line item for detail drawer / expand
public class AffiliatePendingReferralDTO
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;        // join orders.order_number if available
    public decimal OrderTotal { get; set; }
    public decimal CommissionAmount { get; set; }
    public int CommissionRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string MaskedCustomer { get; set; } = string.Empty;     // reuse masking from GetRecentReferrals
}

public class ProcessAffiliatePayoutDTO
{
    public string? PaymentMethod { get; set; }   // default "manual"
    public string? AdminNotes { get; set; }
}

public class AffiliatePayoutResultDTO
{
    public Guid PayoutId { get; set; }
    public string AffiliateCode { get; set; } = string.Empty;
    public string AffiliateName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ReferralCount { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string Status { get; set; } = "completed";
}
```

Wrap in existing `Response<T>` / `PaginatedResponse<T>` patterns.

---

### Service methods

Add to `IAffiliateService` / `AffiliateService`:

```csharp
Task<Response<List<AffiliatePendingPayoutDTO>>> GetAdminPendingPayoutsAsync();
Task<Response<List<AffiliatePendingReferralDTO>>> GetAdminPendingReferralsAsync(string affiliateCode);
Task<Response<AffiliatePayoutResultDTO>> ProcessAdminPayoutAsync(
    string affiliateCode, ProcessAffiliatePayoutDTO request, Guid adminUserId);
Task<Response<PaginatedResponse<AffiliatePayoutResultDTO>>> GetAdminPayoutHistoryAsync(
    int page = 1, int pageSize = 20);
```

#### `ProcessAdminPayoutAsync` logic (transactional)

1. Validate `affiliateCode` exists on an approved affiliate profile.
2. Load all `affiliate_referrals` where `affiliate_code = code AND status = 'pending'`.
3. If none → `400` `"No pending commissions for this affiliate"`.
4. In a **transaction** (or sequential with rollback on failure):
   - Insert `affiliate_payouts` row (`total_amount`, `referral_count`, `processed_by`, `payment_method ?? "manual"`, `admin_notes`).
   - Update each referral: `status = 'paid'`, `paid_at = now()`, `payout_id`, `processed_by`.
5. Return `AffiliatePayoutResultDTO`.

**Idempotency:** Processing twice should fail on second call (no pending left) — do not double-pay.

**Refunds:** If an order is refunded after referral tracked, defer handling to Phase 2 (`cancelled` status + admin tool).

#### `GetAdminPendingPayoutsAsync` query shape

```sql
SELECT
    r.affiliate_code,
    p.full_name,
    p.affiliate_tier,
    SUM(r.commission_amount) AS total_amount,
    COUNT(*) AS referral_count,
    MIN(r.created_at) AS oldest_pending_date
FROM "MuuqWear".affiliate_referrals r
JOIN "MuuqWear".profiles p ON p.affiliate_code = r.affiliate_code
WHERE r.status = 'pending'
  AND p.affiliate_application_status = 'approved'
GROUP BY r.affiliate_code, p.full_name, p.affiliate_tier
ORDER BY oldest_pending_date ASC;
```

---

### Admin badge extension (recommended)

Extend `AffiliateCountsDTO` / `AffiliateCountsModel`:

```csharp
public int PendingPayouts { get; set; }  // COUNT(DISTINCT affiliate_code) WHERE referral status = pending
```

Update `AdminBadgeService.CountAffiliateApplications()` or add `CountPendingPayouts()` helper.

MuuqWear will use this for the Payouts tab badge: `Payouts (N)`.

---

### Validation rules

- `affiliateCode` must match an approved affiliate with ≥1 pending referral
- `ProcessAffiliatePayout`: admin-only; record `processed_by` from JWT
- `paymentMethod` if provided: whitelist `manual`, `paypal`, `bank_transfer`, `store_credit` (only `manual` required for Phase 1)
- `adminNotes` max 2000 chars
- Cannot process if total amount ≤ 0

---

### Files to touch (MuuqWearApi)

| Area | Files |
|------|--------|
| Model | `AffiliatePayout.cs` (new entity) |
| Model | extend `AffiliateReferral.cs` — `PaidAt`, `PayoutId`, `ProcessedBy` |
| DTO | `AffiliatePendingPayoutDTO.cs`, `AffiliatePendingReferralDTO.cs`, `ProcessAffiliatePayoutDTO.cs`, `AffiliatePayoutResultDTO.cs` |
| Service | `IAffiliateService.cs`, `AffiliateService.cs` |
| Controller | `AffiliateController.cs` |
| Badge | `AdminBadgeCountsDTO.cs`, `AdminBadgeService.cs` |
| SQL | `docs/scripts/create-affiliate-payouts.sql`, update seed if needed |

---

### Acceptance criteria

- [ ] After seed, `GET /api/Affiliate/admin/payouts` returns 3 grouped rows (Amara, David, Fatima — Amara aggregated from 2 referrals)
- [ ] Amara row shows `totalAmount = 85.50` (37.50 + 48.00 from seed)
- [ ] `POST /api/Affiliate/admin/payouts/DAVID3921/process` marks David's referral `paid`, creates payout batch, returns result DTO
- [ ] Second process call for same affiliate returns 400 (no pending)
- [ ] `GET /api/Affiliate/admin/payouts` no longer includes processed affiliate
- [ ] Affiliate dashboard `CommissionPending` decreases after process
- [ ] Non-admin receives 403
- [ ] Migration scripts idempotent

---

### Example responses

**GET `/api/Affiliate/admin/payouts`**

```json
{
  "success": true,
  "message": "Pending payouts fetched",
  "data": [
    {
      "affiliateCode": "DAVID3921",
      "affiliateName": "David K.",
      "affiliateTier": "silver",
      "totalAmount": 18.00,
      "referralCount": 1,
      "oldestPendingDate": "2026-07-11T08:00:00Z",
      "status": "pending"
    },
    {
      "affiliateCode": "AMARA2847",
      "affiliateName": "Amara O.",
      "affiliateTier": "gold",
      "totalAmount": 85.50,
      "referralCount": 2,
      "oldestPendingDate": "2026-07-09T08:00:00Z",
      "status": "pending"
    }
  ]
}
```

**POST `/api/Affiliate/admin/payouts/DAVID3921/process`**

Request:
```json
{
  "paymentMethod": "manual",
  "adminNotes": "PayPal batch Mar 2026"
}
```

Response:
```json
{
  "success": true,
  "message": "Payout processed",
  "data": {
    "payoutId": "p0000001-0000-4000-8000-000000000001",
    "affiliateCode": "DAVID3921",
    "affiliateName": "David K.",
    "totalAmount": 18.00,
    "referralCount": 1,
    "processedAt": "2026-07-13T09:30:00Z",
    "status": "completed"
  }
}
```

---

### What Blazor will do after API is ready (MuuqWear — separate task)

1. Add models mirroring DTOs in `MuuqWear.Model/AffiliateApplication/`.
2. Add `IAffiliateService` methods:
   - `GetAdminPendingPayouts()`
   - `GetAdminPendingReferrals(affiliateCode)` (optional expand)
   - `ProcessAdminPayout(affiliateCode, ProcessAffiliatePayoutModel)`
3. Replace Payouts placeholder in `AdminAffiliatesComponent.razor`:
   - List rows matching design (name, amount, date, status)
   - **Process Payout** button per row with loading/disabled state
   - Tab count from `AffiliateCounts.PendingPayouts` or list length
4. Remove readonly banner; show errors from API validation.

**Notify frontend when endpoints are deployed and sample JSON is stable.**

---

### Related existing code (do not break)

| File | Relevance |
|------|-----------|
| `AffiliateService.TrackOrderReferral` | Creates pending referrals — keep as-is |
| `AffiliateService.GetAffiliateInfo` | Uses pending sum — should auto-reflect after process |
| `AffiliateService.GetRecentReferrals` | User dashboard — show `paid` referrals with updated status |
| `seed-affiliate-program.sql` | 4 pending referrals for demo |

---

### Open questions for product/API owner

1. **Grouped vs per-referral rows:** Design shows per-affiliate aggregate (recommended). Confirm.
2. **Minimum payout ($50):** Enforce on admin process, or only when affiliate self-requests (Phase 2)?
3. **Status naming:** Use `paid` in DB vs `completed` in UI — recommend `paid` in API/DB.

---

## End of prompt
