# API Handoff: Support Ticket Drawer (Phase 4)

**Target repo:** `MuuqWearApi`  
**Branch:** stay on `feature/affiliate-tier-settings` (do not switch branches)  
**Consumer:** `MuuqWear.Web` → `/admin/support?tab=tickets`  
**Design reference:** `.design-reference/backend/CustomerSupportV2.tsx` (`TicketDrawer`, `TicketsTab`)  
**Schema:** `"MuuqWear"`

---

## Prompt (copy from here)

Implement **Support Ticket drawer operations** in MuuqWearApi so the Blazor admin tickets tab can open a drawer, assign team/agent, change status/priority, and persist agent replies.

### Background

Web Phase 4 is wired to these routes (see `MuuqWear.Application/Services/HelpCenterService/HelpCenterService.cs`):

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `api/Help/admin/tickets/{ticketId}` | Full ticket + replies |
| `PATCH` | `api/Help/admin/tickets/{ticketId}` | Update status, priority, team, assignment |
| `POST` | `api/Help/admin/tickets/{ticketId}/replies` | Agent reply |
| `POST` | `api/Help/admin/tickets/{ticketId}/assign-me` | Assign current admin + optional status bump |

Existing routes (`GET admin/tickets`, `PATCH .../status`, `GET stats`) stay as-is for backward compatibility.

**Auth:** `[Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]`  
**Supabase client:** service-role via `SupabaseAdminClientFactory` (same as Help articles / chat)

---

### 1. SQL migration

Create `docs/scripts/create-support-ticket-drawer.sql` (idempotent):

**Extend `support_tickets`:**

```sql
assigned_to       uuid NULL,
assigned_to_name  text NULL,
team              text NULL,
first_response_at timestamptz NULL
```

**New table `support_ticket_replies`:**

```sql
id           uuid PK DEFAULT gen_random_uuid()
ticket_id    uuid NOT NULL REFERENCES support_tickets(id) ON DELETE CASCADE
sender_type  text NOT NULL CHECK (sender_type IN ('customer', 'agent'))
sender_id    uuid NULL
sender_name  text NULL
message      text NOT NULL
created_at   timestamptz NOT NULL DEFAULT now()
```

Index: `(ticket_id, created_at ASC)`  
RLS: service_role full access (match help_article_comments pattern)

**Seed original message as first reply (optional):** On ticket submit, you may insert a `customer` reply from `support_tickets.message` for thread continuity, or leave thread empty and let Web show `Message` as “Original message” (current Web behavior).

---

### 2. Models & DTOs

**Extend `SupportTicket` model** with new columns.

**Extend `SupportTicketDTO`:**

```csharp
public string? Team { get; set; }
public Guid? AssignedTo { get; set; }
public string? AssignedToName { get; set; }
public DateTime? FirstResponseAt { get; set; }
public List<SupportTicketReplyDTO> Replies { get; set; } = [];
```

**New DTOs:**

```csharp
public class SupportTicketReplyDTO
{
    public Guid Id { get; set; }
    public string SenderType { get; set; } = "agent"; // customer | agent
    public string? SenderName { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

public class UpdateTicketDTO
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Team { get; set; }           // empty/null = unassigned
    public Guid? AssignedTo { get; set; }
    public string? AssignedToName { get; set; } // empty/null = unassigned
}

public class AddTicketReplyDTO
{
    public string Message { get; set; } = string.Empty;
}
```

JSON: camelCase (match existing API).

---

### 3. Controller (`HelpController`)

Add to existing controller:

```csharp
[HttpPatch("admin/tickets/{ticketId}")]
[Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
Task<ActionResult<Response<SupportTicketDTO>>> UpdateTicket(Guid ticketId, [FromBody] UpdateTicketDTO request);

[HttpPost("admin/tickets/{ticketId}/replies")]
[Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
Task<ActionResult<Response<SupportTicketReplyDTO>>> AddTicketReply(Guid ticketId, [FromBody] AddTicketReplyDTO request);

[HttpPost("admin/tickets/{ticketId}/assign-me")]
[Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
Task<ActionResult<Response<SupportTicketDTO>>> AssignTicketToMe(Guid ticketId);
```

**Extend `GetTicketById`:** load replies ordered by `created_at ASC` and map into DTO.

---

### 4. Service behavior

**`UpdateTicket`**
- Validate status ∈ `open`, `in_progress`, `resolved`
- Validate priority ∈ `high`, `normal`, `low` (add `low` if missing today)
- `team` / `assignedToName`: store `null` when empty or `"Unassigned"`
- Set `updated_at`
- If status → `resolved`, keep assignment fields

**`AddTicketReply`**
- Require non-empty `message`
- Insert reply with `sender_type = agent`, `sender_id` from JWT user id, `sender_name` from claims (name → email → `"Support Agent"`)
- If `first_response_at` is null, set it to `now()`
- If ticket status is `open`, optionally set to `in_progress`
- Return the created `SupportTicketReplyDTO`

**`AssignTicketToMe`**
- Set `assigned_to` = current user id, `assigned_to_name` = display name from claims
- If status is `open`, set `in_progress`
- Return full updated ticket DTO with replies

**`GetAllTickets` RPC/list:** include `team`, `assigned_to_name`, reply count (optional) so list badges work without opening drawer.

---

### 5. Web contract notes

- Web sends PATCH body with only changed fields (camelCase keys: `status`, `priority`, `team`, `assignedTo`, `assignedToName`).
- Agent dropdown uses display names (prototype-style); store `assigned_to_name` even when `assigned_to` is null for v1.
- Public `/help` and customer ticket submit are unchanged.
- Email notifications on reply are **out of scope** (document as future work).

---

### 6. Verification

1. Run SQL scripts in Supabase
2. Restart API
3. `GET api/Help/admin/tickets/{id}` returns `replies[]`
4. `POST .../replies` persists; reload shows thread
5. `PATCH .../tickets/{id}` updates team/agent/status
6. `POST .../assign-me` assigns current admin
7. Blazor: open ticket drawer → reply → reload → thread persists

---

*Consumer branch: `feature/admin` (MuuqWear). Do not edit MuuqWearApi from the Web workspace.*
