-- =============================================================================
-- MUUQWEAR — Affiliate Program admin demo seed
--
-- Source: .design-reference/backend/AffiliateProgram.tsx
--         MuuqWearApi affiliate_applications + profiles + affiliate_referrals
--
-- Target schema: "MuuqWear" (Supabase — same as MuuqWearApi)
-- Tables used:
--   • profiles                 — affiliate tier, codes, commission stats
--   • affiliate_applications   — admin /admin/affiliates?tab=pending|active
--   • orders                   — minimal rows for referral FK (payout demo)
--   • affiliate_referrals      — pending commissions (future Payouts tab)
--   • affiliate_clicks         — optional click stats for active affiliates
--
-- WHAT THIS SEEDS
--   • 4 pending applications (Pending Applications tab)
--   • 2 waitlisted applications (Pending → Waitlisted filter)
--   • 5 approved / active affiliates with tiers + stats (Active tab)
--   • 1 rejected application (optional admin testing)
--   • 4 pending referral commissions (Payouts tab — when API is wired)
--
-- PREREQUISITE
--   profiles.id FK → auth.users.id (shown as "users" in Postgres errors).
--   This script creates auth.users + auth.identities first, then MuuqWear.profiles.
--   Run in Supabase SQL Editor (service role). Safe for local/dev seeding only.
--
-- DEMO LOGIN (optional — all seeded affiliate demo accounts)
--   Password: AffiliateDemo123!
--   Emails:   *@demo.muuqwear.test (see auth insert block below)
--
-- HOW TO RUN
--   Supabase Dashboard → SQL → New query → paste → Run
--
-- TEST AFTER RUN
--   Admin:  /admin/affiliates?tab=pending   → 4 pending + waitlist filter
--   Admin:  /admin/affiliates?tab=active    → 5 active (453/500 cap uses badge count)
--   API:    GET /api/Affiliate/admin/applications?status=pending
--   API:    GET /api/AdminBadge/counts       → AffiliateCounts populated
--
-- IDEMPOTENT — fixed UUIDs + ON CONFLICT DO UPDATE
-- =============================================================================

BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ---------------------------------------------------------------------------
-- Demo user / profile IDs (replace if they clash with real users)
-- ---------------------------------------------------------------------------
-- Pending applicants
--   a0000001  Sarah K.
--   a0000002  Marcus C.
--   a0000003  Elena R.
--   a0000004  Priya D.
-- Waitlisted
--   a0000005  Jordan M.
--   a0000006  Aisha N.
-- Active affiliates
--   a0000007  Amara O.   (gold)
--   a0000008  David K.    (silver)
--   a0000009  Fatima H.   (gold)
--   a000000a  James P.    (bronze)
--   a000000b  Lisa T.     (silver)
-- Rejected sample
--   a000000c  Ryan L.

-- ---------------------------------------------------------------------------
-- 0) auth.users + auth.identities (required before MuuqWear.profiles)
--    If you have an on_auth_user_created trigger, it may insert a bare profile
--    row first — the profiles upsert below fills affiliate fields.
-- ---------------------------------------------------------------------------

INSERT INTO auth.users (
    id,
    instance_id,
    aud,
    role,
    email,
    encrypted_password,
    email_confirmed_at,
    recovery_sent_at,
    last_sign_in_at,
    raw_app_meta_data,
    raw_user_meta_data,
    created_at,
    updated_at,
    confirmation_token,
    email_change,
    email_change_token_new,
    recovery_token
)
SELECT
    u.id,
    '00000000-0000-0000-0000-000000000000'::uuid,
    'authenticated',
    'authenticated',
    u.email,
    crypt('AffiliateDemo123!', gen_salt('bf')),
    now(),
    now(),
    now(),
    '{"provider":"email","providers":["email"]}'::jsonb,
    jsonb_build_object('full_name', u.full_name),
    now(),
    now(),
    '',
    '',
    '',
    ''
FROM (VALUES
    ('a0000001-0000-4000-8000-000000000001'::uuid, 'sarah.k@demo.muuqwear.test', 'Sarah K.'),
    ('a0000002-0000-4000-8000-000000000002'::uuid, 'marcus.c@demo.muuqwear.test', 'Marcus C.'),
    ('a0000003-0000-4000-8000-000000000003'::uuid, 'elena.r@demo.muuqwear.test', 'Elena R.'),
    ('a0000004-0000-4000-8000-000000000004'::uuid, 'priya.d@demo.muuqwear.test', 'Priya D.'),
    ('a0000005-0000-4000-8000-000000000005'::uuid, 'jordan.m@demo.muuqwear.test', 'Jordan M.'),
    ('a0000006-0000-4000-8000-000000000006'::uuid, 'aisha.n@demo.muuqwear.test', 'Aisha N.'),
    ('a0000007-0000-4000-8000-000000000007'::uuid, 'amara.o@demo.muuqwear.test', 'Amara O.'),
    ('a0000008-0000-4000-8000-000000000008'::uuid, 'david.k@demo.muuqwear.test', 'David K.'),
    ('a0000009-0000-4000-8000-000000000009'::uuid, 'fatima.h@demo.muuqwear.test', 'Fatima H.'),
    ('a000000a-0000-4000-8000-00000000000a'::uuid, 'james.p@demo.muuqwear.test', 'James P.'),
    ('a000000b-0000-4000-8000-00000000000b'::uuid, 'lisa.t@demo.muuqwear.test', 'Lisa T.'),
    ('a000000c-0000-4000-8000-00000000000c'::uuid, 'ryan.l@demo.muuqwear.test', 'Ryan L.')
) AS u(id, email, full_name)
ON CONFLICT (id) DO UPDATE SET
    email = EXCLUDED.email,
    raw_user_meta_data = EXCLUDED.raw_user_meta_data,
    updated_at = now();

DELETE FROM auth.identities
WHERE user_id IN (
    'a0000001-0000-4000-8000-000000000001'::uuid,
    'a0000002-0000-4000-8000-000000000002'::uuid,
    'a0000003-0000-4000-8000-000000000003'::uuid,
    'a0000004-0000-4000-8000-000000000004'::uuid,
    'a0000005-0000-4000-8000-000000000005'::uuid,
    'a0000006-0000-4000-8000-000000000006'::uuid,
    'a0000007-0000-4000-8000-000000000007'::uuid,
    'a0000008-0000-4000-8000-000000000008'::uuid,
    'a0000009-0000-4000-8000-000000000009'::uuid,
    'a000000a-0000-4000-8000-00000000000a'::uuid,
    'a000000b-0000-4000-8000-00000000000b'::uuid,
    'a000000c-0000-4000-8000-00000000000c'::uuid
);

INSERT INTO auth.identities (
    id,
    user_id,
    provider_id,
    identity_data,
    provider,
    last_sign_in_at,
    created_at,
    updated_at
)
SELECT
    u.id,
    u.id,
    u.email,
    jsonb_build_object(
        'sub', u.id::text,
        'email', u.email,
        'email_verified', true,
        'phone_verified', false
    ),
    'email',
    now(),
    now(),
    now()
FROM (VALUES
    ('a0000001-0000-4000-8000-000000000001'::uuid, 'sarah.k@demo.muuqwear.test'),
    ('a0000002-0000-4000-8000-000000000002'::uuid, 'marcus.c@demo.muuqwear.test'),
    ('a0000003-0000-4000-8000-000000000003'::uuid, 'elena.r@demo.muuqwear.test'),
    ('a0000004-0000-4000-8000-000000000004'::uuid, 'priya.d@demo.muuqwear.test'),
    ('a0000005-0000-4000-8000-000000000005'::uuid, 'jordan.m@demo.muuqwear.test'),
    ('a0000006-0000-4000-8000-000000000006'::uuid, 'aisha.n@demo.muuqwear.test'),
    ('a0000007-0000-4000-8000-000000000007'::uuid, 'amara.o@demo.muuqwear.test'),
    ('a0000008-0000-4000-8000-000000000008'::uuid, 'david.k@demo.muuqwear.test'),
    ('a0000009-0000-4000-8000-000000000009'::uuid, 'fatima.h@demo.muuqwear.test'),
    ('a000000a-0000-4000-8000-00000000000a'::uuid, 'james.p@demo.muuqwear.test'),
    ('a000000b-0000-4000-8000-00000000000b'::uuid, 'lisa.t@demo.muuqwear.test'),
    ('a000000c-0000-4000-8000-00000000000c'::uuid, 'ryan.l@demo.muuqwear.test')
) AS u(id, email);

-- ---------------------------------------------------------------------------
-- 1) Profiles — affiliate fields for demo users
-- ---------------------------------------------------------------------------

INSERT INTO "MuuqWear".profiles (
    id,
    full_name,
    email,
    role,
    created_at,
    affiliate_tier,
    affiliate_items_sold,
    affiliate_commission_earned,
    affiliate_bonus_earned,
    affiliate_application_status,
    affiliate_code,
    affiliate_total_clicks
) VALUES
    -- Pending (no affiliate code yet)
    ('a0000001-0000-4000-8000-000000000001', 'Sarah K.', 'sarah.k@demo.muuqwear.test', 'customer', now() - interval '30 days', 'none', 0, 0, 0, 'pending', NULL, 0),
    ('a0000002-0000-4000-8000-000000000002', 'Marcus C.', 'marcus.c@demo.muuqwear.test', 'customer', now() - interval '28 days', 'none', 0, 0, 0, 'pending', NULL, 0),
    ('a0000003-0000-4000-8000-000000000003', 'Elena R.', 'elena.r@demo.muuqwear.test', 'customer', now() - interval '25 days', 'none', 0, 0, 0, 'pending', NULL, 0),
    ('a0000004-0000-4000-8000-000000000004', 'Priya D.', 'priya.d@demo.muuqwear.test', 'customer', now() - interval '22 days', 'none', 0, 0, 0, 'pending', NULL, 0),
    -- Waitlisted
    ('a0000005-0000-4000-8000-000000000005', 'Jordan M.', 'jordan.m@demo.muuqwear.test', 'customer', now() - interval '20 days', 'none', 0, 0, 0, 'waitlisted', NULL, 0),
    ('a0000006-0000-4000-8000-000000000006', 'Aisha N.', 'aisha.n@demo.muuqwear.test', 'customer', now() - interval '18 days', 'none', 0, 0, 0, 'waitlisted', NULL, 0),
    -- Active / approved
    ('a0000007-0000-4000-8000-000000000007', 'Amara O.', 'amara.o@demo.muuqwear.test', 'customer', now() - interval '180 days', 'gold', 1234, 12450.00, 500.00, 'approved', 'AMARA2847', 8420),
    ('a0000008-0000-4000-8000-000000000008', 'David K.', 'david.k@demo.muuqwear.test', 'customer', now() - interval '120 days', 'silver', 312, 3240.00, 0, 'approved', 'DAVID3921', 2100),
    ('a0000009-0000-4000-8000-000000000009', 'Fatima H.', 'fatima.h@demo.muuqwear.test', 'customer', now() - interval '90 days', 'gold', 890, 9870.00, 250.00, 'approved', 'FATIMA5512', 3900),
    ('a000000a-0000-4000-8000-00000000000a', 'James P.', 'james.p@demo.muuqwear.test', 'customer', now() - interval '60 days', 'bronze', 78, 890.00, 0, 'approved', 'JAMES7781', 640),
    ('a000000b-0000-4000-8000-00000000000b', 'Lisa T.', 'lisa.t@demo.muuqwear.test', 'customer', now() - interval '45 days', 'silver', 245, 2180.00, 0, 'approved', 'LISA9044', 1100),
    -- Rejected
    ('a000000c-0000-4000-8000-00000000000c', 'Ryan L.', 'ryan.l@demo.muuqwear.test', 'customer', now() - interval '14 days', 'none', 0, 0, 0, 'rejected', NULL, 0)
ON CONFLICT (id) DO UPDATE SET
    full_name = EXCLUDED.full_name,
    email = EXCLUDED.email,
    affiliate_tier = EXCLUDED.affiliate_tier,
    affiliate_items_sold = EXCLUDED.affiliate_items_sold,
    affiliate_commission_earned = EXCLUDED.affiliate_commission_earned,
    affiliate_bonus_earned = EXCLUDED.affiliate_bonus_earned,
    affiliate_application_status = EXCLUDED.affiliate_application_status,
    affiliate_code = EXCLUDED.affiliate_code,
    affiliate_total_clicks = EXCLUDED.affiliate_total_clicks;

-- ---------------------------------------------------------------------------
-- 2) Affiliate applications
-- ---------------------------------------------------------------------------

INSERT INTO "MuuqWear".affiliate_applications (
    id,
    user_id,
    full_name,
    email,
    social_handles,
    audience_size,
    content_niche,
    portfolio_url,
    why_muuqwear,
    sample_files,
    status,
    submitted_at,
    reviewed_at,
    admin_notes
) VALUES
    (
        'b0000001-0000-4000-8000-000000000001',
        'a0000001-0000-4000-8000-000000000001',
        'Sarah K.',
        'sarah.k@demo.muuqwear.test',
        '[{"platform":"Instagram","handle":"sarahkstyle","followers":12400},{"platform":"TikTok","handle":"sarahkstyle","followers":8200}]'::jsonb,
        12400,
        'Streetwear Fashion',
        'https://instagram.com/sarahkstyle',
        'Muuqwear aligns with my audience who love elevated streetwear and cultural storytelling.',
        '[]'::jsonb,
        'pending',
        '2025-03-22 10:00:00+00',
        NULL,
        NULL
    ),
    (
        'b0000002-0000-4000-8000-000000000002',
        'a0000002-0000-4000-8000-000000000002',
        'Marcus C.',
        'marcus.c@demo.muuqwear.test',
        '[{"platform":"Instagram","handle":"marcuschen","followers":45200},{"platform":"YouTube","handle":"marcuschen","followers":28000}]'::jsonb,
        45200,
        'Menswear & Lifestyle',
        'https://instagram.com/marcuschen',
        'I create long-form menswear content and Muuqwear fits my premium positioning.',
        '[]'::jsonb,
        'pending',
        '2025-03-21 14:30:00+00',
        NULL,
        NULL
    ),
    (
        'b0000003-0000-4000-8000-000000000003',
        'a0000003-0000-4000-8000-000000000003',
        'Elena R.',
        'elena.r@demo.muuqwear.test',
        '[{"platform":"Instagram","handle":"elenar.style","followers":8900}]'::jsonb,
        8900,
        'Minimalist Fashion',
        'https://instagram.com/elenar.style',
        'My community values quality basics and thoughtful design — perfect for Muuqwear.',
        '[]'::jsonb,
        'pending',
        '2025-03-20 09:15:00+00',
        NULL,
        NULL
    ),
    (
        'b0000004-0000-4000-8000-000000000004',
        'a0000004-0000-4000-8000-000000000004',
        'Priya D.',
        'priya.d@demo.muuqwear.test',
        '[{"platform":"Instagram","handle":"priyadesigns","followers":23100},{"platform":"TikTok","handle":"priyadesigns","followers":19000}]'::jsonb,
        23100,
        'Sustainable Fashion',
        'https://instagram.com/priyadesigns',
        'Sustainability is core to my brand and I want to partner with Muuqwear on conscious style.',
        '[]'::jsonb,
        'pending',
        '2025-03-19 16:45:00+00',
        NULL,
        NULL
    ),
    (
        'b0000005-0000-4000-8000-000000000005',
        'a0000005-0000-4000-8000-000000000005',
        'Jordan M.',
        'jordan.m@demo.muuqwear.test',
        '[{"platform":"Instagram","handle":"jordanmfits","followers":15600}]'::jsonb,
        15600,
        'Athleisure',
        NULL,
        'Waitlisted while program capacity opens up.',
        '[]'::jsonb,
        'waitlisted',
        '2025-03-18 11:00:00+00',
        '2025-03-18 12:00:00+00',
        'Auto-waitlisted — program at capacity.'
    ),
    (
        'b0000006-0000-4000-8000-000000000006',
        'a0000006-0000-4000-8000-000000000006',
        'Aisha N.',
        'aisha.n@demo.muuqwear.test',
        '[{"platform":"TikTok","handle":"aishastyle","followers":9800}]'::jsonb,
        9800,
        'Modest Fashion',
        NULL,
        'Interested in representing Muuqwear to modest fashion communities.',
        '[]'::jsonb,
        'waitlisted',
        '2025-03-17 08:30:00+00',
        '2025-03-17 09:00:00+00',
        'Strong fit — waitlist for next cohort.'
    ),
    (
        'b0000007-0000-4000-8000-000000000007',
        'a0000007-0000-4000-8000-000000000007',
        'Amara O.',
        'amara.o@demo.muuqwear.test',
        '[{"platform":"Instagram","handle":"amarao","followers":62000}]'::jsonb,
        62000,
        'Luxury Streetwear',
        'https://instagram.com/amarao',
        'Approved affiliate — top performer.',
        '[]'::jsonb,
        'approved',
        '2024-09-01 10:00:00+00',
        '2024-09-03 15:00:00+00',
        NULL
    ),
    (
        'b0000008-0000-4000-8000-000000000008',
        'a0000008-0000-4000-8000-000000000008',
        'David K.',
        'david.k@demo.muuqwear.test',
        '[{"platform":"YouTube","handle":"davidkstyle","followers":31000}]'::jsonb,
        31000,
        'Menswear',
        NULL,
        'Approved — consistent mid-tier performer.',
        '[]'::jsonb,
        'approved',
        '2024-11-10 12:00:00+00',
        '2024-11-12 09:00:00+00',
        NULL
    ),
    (
        'b0000009-0000-4000-8000-000000000009',
        'a0000009-0000-4000-8000-000000000009',
        'Fatima H.',
        'fatima.h@demo.muuqwear.test',
        '[{"platform":"Instagram","handle":"fatimah","followers":41000}]'::jsonb,
        41000,
        'Culture & Fashion',
        NULL,
        'Approved — strong conversion rate.',
        '[]'::jsonb,
        'approved',
        '2024-12-05 14:00:00+00',
        '2024-12-06 10:00:00+00',
        NULL
    ),
    (
        'b000000a-0000-4000-8000-00000000000a',
        'a000000a-0000-4000-8000-00000000000a',
        'James P.',
        'james.p@demo.muuqwear.test',
        '[{"platform":"Instagram","handle":"jamespfits","followers":5400}]'::jsonb,
        5400,
        'Fitness Fashion',
        NULL,
        'Approved — bronze tier starter.',
        '[]'::jsonb,
        'approved',
        '2025-01-20 09:00:00+00',
        '2025-01-21 11:00:00+00',
        NULL
    ),
    (
        'b000000b-0000-4000-8000-00000000000b',
        'a000000b-0000-4000-8000-00000000000b',
        'Lisa T.',
        'lisa.t@demo.muuqwear.test',
        '[{"platform":"TikTok","handle":"lisatstyle","followers":18700}]'::jsonb,
        18700,
        'Lifestyle',
        NULL,
        'Approved — silver tier.',
        '[]'::jsonb,
        'approved',
        '2025-02-14 13:00:00+00',
        '2025-02-15 10:00:00+00',
        NULL
    ),
    (
        'b000000c-0000-4000-8000-00000000000c',
        'a000000c-0000-4000-8000-00000000000c',
        'Ryan L.',
        'ryan.l@demo.muuqwear.test',
        '[{"platform":"Instagram","handle":"ryanl","followers":450}]'::jsonb,
        450,
        'General Fashion',
        NULL,
        'Does not meet minimum audience requirements.',
        '[]'::jsonb,
        'rejected',
        '2025-03-10 10:00:00+00',
        '2025-03-11 09:00:00+00',
        'Audience below 100 follower minimum.'
    )
ON CONFLICT (id) DO UPDATE SET
    user_id = EXCLUDED.user_id,
    full_name = EXCLUDED.full_name,
    email = EXCLUDED.email,
    social_handles = EXCLUDED.social_handles,
    audience_size = EXCLUDED.audience_size,
    content_niche = EXCLUDED.content_niche,
    portfolio_url = EXCLUDED.portfolio_url,
    why_muuqwear = EXCLUDED.why_muuqwear,
    sample_files = EXCLUDED.sample_files,
    status = EXCLUDED.status,
    submitted_at = EXCLUDED.submitted_at,
    reviewed_at = EXCLUDED.reviewed_at,
    admin_notes = EXCLUDED.admin_notes;

-- ---------------------------------------------------------------------------
-- 3) Minimal orders — backing rows for pending payout referrals
-- ---------------------------------------------------------------------------

INSERT INTO "MuuqWear".orders (
    id,
    order_number,
    user_id,
    email,
    subtotal,
    shipping,
    tax,
    total,
    status,
    first_name,
    last_name,
    payment_status,
    created_at,
    pending_affiliate_code
) VALUES
    ('d0000001-0000-4000-8000-000000000001', 'MQ-AFF-SEED-001', 'a0000001-0000-4000-8000-000000000001', 'buyer1@demo.muuqwear.test', 250.00, 0, 0, 250.00, 'delivered', 'Demo', 'Buyer', 'paid', now() - interval '1 day', 'AMARA2847'),
    ('d0000002-0000-4000-8000-000000000002', 'MQ-AFF-SEED-002', 'a0000002-0000-4000-8000-000000000002', 'buyer2@demo.muuqwear.test', 180.00, 0, 0, 180.00, 'delivered', 'Demo', 'Buyer', 'paid', now() - interval '2 days', 'DAVID3921'),
    ('d0000003-0000-4000-8000-000000000003', 'MQ-AFF-SEED-003', 'a0000003-0000-4000-8000-000000000003', 'buyer3@demo.muuqwear.test', 125.00, 0, 0, 125.00, 'delivered', 'Demo', 'Buyer', 'paid', now() - interval '3 days', 'FATIMA5512'),
    ('d0000004-0000-4000-8000-000000000004', 'MQ-AFF-SEED-004', 'a0000004-0000-4000-8000-000000000004', 'buyer4@demo.muuqwear.test', 320.00, 0, 0, 320.00, 'delivered', 'Demo', 'Buyer', 'paid', now() - interval '4 days', 'AMARA2847')
ON CONFLICT (id) DO UPDATE SET
    order_number = EXCLUDED.order_number,
    total = EXCLUDED.total,
    status = EXCLUDED.status,
    payment_status = EXCLUDED.payment_status,
    pending_affiliate_code = EXCLUDED.pending_affiliate_code;

-- ---------------------------------------------------------------------------
-- 4) Pending affiliate referrals
--    Commission rates: bronze 5%, silver 10%, gold 15%
-- ---------------------------------------------------------------------------

INSERT INTO "MuuqWear".affiliate_referrals (
    id,
    order_id,
    affiliate_code,
    user_id,
    order_total,
    commission_amount,
    commission_rate,
    status,
    created_at
) VALUES
    ('c0000001-0000-4000-8000-000000000001', 'd0000001-0000-4000-8000-000000000001', 'AMARA2847', 'a0000001-0000-4000-8000-000000000001', 250.00, 37.50, 15, 'pending', now() - interval '1 day'),
    ('c0000002-0000-4000-8000-000000000002', 'd0000002-0000-4000-8000-000000000002', 'DAVID3921', 'a0000002-0000-4000-8000-000000000002', 180.00, 18.00, 10, 'pending', now() - interval '2 days'),
    ('c0000003-0000-4000-8000-000000000003', 'd0000003-0000-4000-8000-000000000003', 'FATIMA5512', 'a0000003-0000-4000-8000-000000000003', 125.00, 18.75, 15, 'pending', now() - interval '3 days'),
    ('c0000004-0000-4000-8000-000000000004', 'd0000004-0000-4000-8000-000000000004', 'AMARA2847', 'a0000004-0000-4000-8000-000000000004', 320.00, 48.00, 15, 'pending', now() - interval '4 days')
ON CONFLICT (id) DO UPDATE SET
    order_id = EXCLUDED.order_id,
    affiliate_code = EXCLUDED.affiliate_code,
    order_total = EXCLUDED.order_total,
    commission_amount = EXCLUDED.commission_amount,
    commission_rate = EXCLUDED.commission_rate,
    status = EXCLUDED.status,
    created_at = EXCLUDED.created_at;

-- ---------------------------------------------------------------------------
-- 5) Sample affiliate clicks (optional — dashboard / analytics)
-- ---------------------------------------------------------------------------

INSERT INTO "MuuqWear".affiliate_clicks (
    id,
    affiliate_code,
    clicked_at,
    ip_address,
    user_agent,
    referrer_url,
    converted
) VALUES
    ('f0000001-0000-4000-8000-000000000001', 'AMARA2847', now() - interval '2 hours', '203.0.113.10', 'Mozilla/5.0 (demo)', 'https://google.com', false),
    ('f0000002-0000-4000-8000-000000000002', 'DAVID3921', now() - interval '5 hours', '203.0.113.11', 'Mozilla/5.0 (demo)', 'https://instagram.com', true),
    ('f0000003-0000-4000-8000-000000000003', 'FATIMA5512', now() - interval '1 day', '203.0.113.12', 'Mozilla/5.0 (demo)', NULL, false)
ON CONFLICT (id) DO UPDATE SET
    affiliate_code = EXCLUDED.affiliate_code,
    clicked_at = EXCLUDED.clicked_at,
    converted = EXCLUDED.converted;

COMMIT;

-- ---------------------------------------------------------------------------
-- Verification queries
-- ---------------------------------------------------------------------------
-- SELECT status, count(*) FROM "MuuqWear".affiliate_applications GROUP BY status ORDER BY status;
-- SELECT affiliate_tier, count(*) FROM "MuuqWear".profiles WHERE affiliate_tier <> 'none' GROUP BY affiliate_tier;
-- SELECT affiliate_code, sum(commission_amount) AS pending_total
--   FROM "MuuqWear".affiliate_referrals WHERE status = 'pending' GROUP BY affiliate_code;
