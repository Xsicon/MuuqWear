-- =============================================================================
-- MUUQWEAR — Muuqsimo 2026 sample data seed (multi-event demo)
--
-- Companion to: docs/scripts/seed-muuqsimo-2025.sql
--
-- WHAT THIS DEMONSTRATES
--   • Multiple rows in "MuuqWear".events (admin → Content → Events)
--   • Each event has its own `content.slug` and optional ticket product GUIDs
--   • API resolves by slug: GET /api/Muuqsimo?slug=muuqsimo-2026
--   • If slug is missing, API falls back to the most recently published event
--   • Draft events appear in admin only (status = 'draft')
--
-- PREREQUISITE
--   Run seed-muuqsimo-2025.sql first (optional — this script is standalone)
--
-- HOW TO RUN
--   Supabase Dashboard → SQL → New query → paste → Run
--
-- TEST AFTER RUN
--   Admin:  /admin/content?view=events  → 3 events (2 published, 1 draft)
--   API:    GET /api/Muuqsimo?slug=muuqsimo-2026
--   API:    GET /api/Muuqsimo?slug=muuqsimo-2025  (2025 NYC gala)
--   Store:  /muuqsimo still loads muuqsimo-2025 until slug routing is added
--
-- IDEMPOTENT — fixed UUIDs + ON CONFLICT DO UPDATE
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 1) Muuqsimo 2026 — London edition (published)
--    slug: muuqsimo-2026
-- ---------------------------------------------------------------------------

INSERT INTO "MuuqWear".events (
    id,
    title,
    content,
    status,
    views,
    created_at,
    published_at
) VALUES (
    'e0000002-0000-4000-8000-000000000002',
    'Muuqsimo 2026: London',
    $json${
  "slug": "muuqsimo-2026",
  "tagline": "The Veil Crosses the Atlantic",
  "eyebrow": "European Flagship · Year Two",
  "subtitle": "Midnight on the Thames",
  "startDate": "2026-06-12T19:00:00+01:00",
  "endDate": "2026-06-14T23:59:59+01:00",
  "countdownUtc": "2026-06-12T18:00:00Z",
  "venue": {
    "name": "Tate Modern Turbine Hall",
    "address": "Bankside, London SE1 9TG, United Kingdom",
    "accessibility": "Step-free access via River entrance",
    "transit": "Blackfriars / Southwark Tube",
    "parking": "Limited — pre-book Q-Park Bankside"
  },
  "dressCode": {
    "theme": "Midnight Meridian",
    "description": "Black-tie optional with sapphire, silver, or ivory accents. Muuqwear pieces encouraged."
  },
  "heroSlides": [
    {
      "imageUrl": "https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?w=1400&h=800&fit=crop",
      "alt": "London skyline at dusk"
    },
    {
      "imageUrl": "https://images.unsplash.com/photo-1529655683829-aba9b772e9f1?w=1400&h=800&fit=crop",
      "alt": "Tate Modern exterior"
    }
  ],
  "experience": {
    "eyebrow": "The Experience",
    "title": "Where Heritage Meets the Future of Muuqwear",
    "body": "Following the success of Muuqsimo New York, the brand brings its flagship gala to London — a night of runway, awards, and community on the banks of the Thames.",
    "cards": [
      {
        "title": "River Arrival",
        "imageUrl": "https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=600&h=400&fit=crop"
      },
      {
        "title": "Turbine Runway",
        "imageUrl": "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600&h=400&fit=crop"
      },
      {
        "title": "Affiliate Awards",
        "imageUrl": "https://images.unsplash.com/photo-1632679090212-612ac1f4d76f?w=600&h=400&fit=crop"
      }
    ]
  },
  "blueVeilWalk": {
    "eyebrow": "The Arrival",
    "title": "The Meridian Walk",
    "body": "Guests descend into the Turbine Hall along a mirrored sapphire runway reflecting the Thames night sky — captured by a dedicated London press pool.",
    "imageUrl": "https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=600&h=800&fit=crop",
    "specs": [
      { "label": "Material", "value": "Reflective Sapphire Mesh" },
      { "label": "Length", "value": "40 Meters" },
      { "label": "Photography", "value": "Getty Images London" },
      { "label": "Gallery", "value": "Private link within 72h" }
    ]
  },
  "schedule": [
    {
      "icon": "sparkles",
      "title": "Meridian Arrivals",
      "time": "7:00 PM",
      "location": "River Entrance",
      "description": "Champagne welcome and press photos on the mirrored runner."
    },
    {
      "icon": "shirt",
      "title": "Runway: \"Atlantic Veil\"",
      "time": "8:00 PM",
      "location": "Turbine Hall",
      "description": "18 looks spanning Essentials, Technical, and a London-only capsule."
    },
    {
      "icon": "trophy",
      "title": "European Affiliate Awards",
      "time": "9:15 PM",
      "location": "Turbine Hall",
      "description": "Honouring top EU/UK partners and rising creators."
    },
    {
      "icon": "wine",
      "title": "Thames Reception",
      "time": "10:00 PM",
      "location": "Level 6 Terrace",
      "description": "Open bar with skyline views and live string ensemble."
    }
  ],
  "runwayShow": {
    "eyebrow": "The Runway Show",
    "title": "Atlantic Veil",
    "subtitle": "18 looks debuting the SS27 preview and a London-exclusive capsule.",
    "collections": [
      {
        "number": "01",
        "title": "Essentials Revisited",
        "description": "Refined core pieces with British tailoring influences."
      },
      {
        "number": "02",
        "title": "Technical Voyager",
        "description": "Travel-ready layers engineered for transatlantic climate."
      },
      {
        "number": "03",
        "title": "Thames Capsule",
        "description": "Limited midnight-dyed pieces available only at Muuqsimo London."
      }
    ],
    "featuredModels": [
      { "initials": "EN", "name": "Ella Nguyen", "credit": "Muuqwear EU Ambassador" },
      { "initials": "JW", "name": "James Whitfield", "credit": "London Fashion Week" }
    ],
    "modelsExtra": "+ 14 additional models",
    "musicLighting": [
      {
        "icon": "music",
        "title": "Live Strings",
        "description": "Royal Academy musicians for the opening sequence."
      },
      {
        "icon": "sparkles",
        "title": "Meridian Lighting",
        "description": "Cool blue wash transitioning to warm gold per segment."
      }
    ],
    "production": [
      { "role": "Sound", "provider": "Production Park UK" },
      { "role": "Venue", "provider": "Tate Modern Events" },
      { "role": "Catering", "provider": "Rocket Food" }
    ]
  },
  "awards": [
    { "icon": "crown", "title": "EU Affiliate of the Year", "prize": "€20,000 + Milan showroom visit" },
    { "icon": "star", "title": "UK Rising Star", "prize": "€5,000 + mentorship with founders" },
    { "icon": "users", "title": "Community Choice (Europe)", "prize": "€7,500 + capsule collab" }
  ],
  "tickets": {
    "capacityNote": "Limited to 350 Attendees",
    "subtitle": "All tiers include runway, awards, and Thames reception.",
    "tierConfigs": [
      {
        "productId": "44444444-4444-4444-4444-444444444444",
        "totalCapacity": 180,
        "perks": ["Standard admission", "Welcome drink", "Runway & awards"],
        "isHighlighted": false,
        "badge": null
      },
      {
        "productId": "55555555-5555-5555-5555-555555555555",
        "totalCapacity": 120,
        "perks": ["Priority seating", "Gift bag", "Terrace reception access", "All Thames Entry perks"],
        "isHighlighted": true,
        "badge": "Most Popular"
      },
      {
        "productId": "66666666-6666-6666-6666-666666666666",
        "totalCapacity": 50,
        "perks": ["Front row", "Founder meet-and-greet", "Hotel partner upgrade", "All Midnight Circle perks"],
        "isHighlighted": false,
        "badge": null
      }
    ],
    "affiliateBenefits": [
      { "tier": "Bronze", "benefit": "20% off Thames Entry", "dotColor": "#CD7F32" },
      { "tier": "Silver", "benefit": "Complimentary Midnight Circle ticket", "dotColor": "#A9B7CC" },
      { "tier": "Gold", "benefit": "VIP London weekend (hotel + gala)", "dotColor": "#D4A843" }
    ],
    "pressContact": "press@muuqwear.com"
  },
  "team": [
    { "name": "Jordan Muuq", "role": "Founder & Creative Director" },
    { "name": "Elena Veil", "role": "Co-Founder & Creative Director" },
    { "name": "Sophia Laurent", "role": "Event Director" },
    { "name": "Oliver Finch", "role": "UK Event Producer" }
  ],
  "sponsors": [
    { "role": "Presenting Sponsor", "name": "Harbour & Co." },
    { "role": "Hospitality Sponsor", "name": "The Thames Collective" }
  ],
  "closingCta": {
    "eyebrow": "London · June 2026",
    "title": "The Veil Meets the Thames",
    "imageUrl": "https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?w=1400&h=800&fit=crop"
  }
}$json$,
    'published',
    412,
    '2026-01-10 10:00:00+00',
    '2026-02-01 09:00:00+00'
)
ON CONFLICT (id) DO UPDATE SET
    title        = EXCLUDED.title,
    content      = EXCLUDED.content,
    status       = EXCLUDED.status,
    views        = EXCLUDED.views,
    published_at = EXCLUDED.published_at;

-- ---------------------------------------------------------------------------
-- 2) Muuqsimo 2027 — Tokyo preview (draft — admin only)
--    slug: muuqsimo-2027  |  not returned unless published
-- ---------------------------------------------------------------------------

INSERT INTO "MuuqWear".events (
    id,
    title,
    content,
    status,
    views,
    created_at,
    published_at
) VALUES (
    'e0000003-0000-4000-8000-000000000003',
    'Muuqsimo 2027: Tokyo (Preview)',
    $json${
  "slug": "muuqsimo-2027",
  "tagline": "The Veil Rises in the East",
  "eyebrow": "Coming Soon · Draft",
  "subtitle": "Shibuya · Spring 2027",
  "startDate": "2027-04-18T18:00:00+09:00",
  "endDate": "2027-04-20T23:59:59+09:00",
  "countdownUtc": "2027-04-18T09:00:00Z",
  "venue": {
    "name": "TeamLab Borderless Tokyo",
    "address": "Azabudai Hills, Minato City, Tokyo",
    "accessibility": "TBD",
    "transit": "Kamiyacho Station",
    "parking": "TBD"
  },
  "dressCode": {
    "theme": "Neon Veil",
    "description": "Details to be announced."
  },
  "heroSlides": [
    {
      "imageUrl": "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=1400&h=800&fit=crop",
      "alt": "Tokyo night skyline"
    }
  ],
  "experience": {
    "eyebrow": "The Experience",
    "title": "Asia-Pacific Flagship — Work in Progress",
    "body": "Draft placeholder for the third Muuqsimo gala. Edit in admin when ready to announce.",
    "cards": []
  },
  "blueVeilWalk": {
    "eyebrow": "The Arrival",
    "title": "TBD",
    "body": "Draft content.",
    "imageUrl": "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=600&h=800&fit=crop",
    "specs": []
  },
  "schedule": [],
  "runwayShow": {
    "eyebrow": "The Runway Show",
    "title": "TBD",
    "subtitle": "Draft.",
    "collections": [],
    "featuredModels": [],
    "modelsExtra": null,
    "musicLighting": [],
    "production": []
  },
  "awards": [],
  "tickets": {
    "capacityNote": "TBD",
    "subtitle": "Ticket tiers not configured yet.",
    "tierConfigs": [],
    "affiliateBenefits": [],
    "pressContact": "press@muuqwear.com"
  },
  "team": [],
  "sponsors": [],
  "closingCta": {
    "eyebrow": "Tokyo · 2027",
    "title": "Coming Soon",
    "imageUrl": "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=1400&h=800&fit=crop"
  }
}$json$,
    'draft',
    0,
    '2026-03-01 12:00:00+00',
    NULL
)
ON CONFLICT (id) DO UPDATE SET
    title        = EXCLUDED.title,
    content      = EXCLUDED.content,
    status       = EXCLUDED.status,
    views        = EXCLUDED.views,
    published_at = EXCLUDED.published_at;

-- ---------------------------------------------------------------------------
-- 3) Ticket products — Muuqsimo 2026 London (separate GUIDs from 2025)
-- ---------------------------------------------------------------------------

INSERT INTO "MuuqWear".products (
    id,
    name,
    price,
    badge,
    image_url,
    category,
    is_active,
    is_ticket,
    is_new_arrival,
    is_featured,
    is_best_seller,
    description,
    sku,
    color_options,
    is_deleted,
    created_at
) VALUES
(
    '44444444-4444-4444-4444-444444444444',
    'Thames Entry',
    275.00,
    NULL,
    'https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?w=800&h=600&fit=crop',
    'Events',
    true,
    true,
    false,
    false,
    false,
    'Muuqsimo 2026 London standard admission.',
    'MS26-TKT-THAMES',
    '["Midnight"]'::jsonb,
    false,
    '2026-01-10 10:00:00+00'
),
(
    '55555555-5555-5555-5555-555555555555',
    'Midnight Circle',
    525.00,
    'Most Popular',
    'https://images.unsplash.com/photo-1529655683829-aba9b772e9f1?w=800&h=600&fit=crop',
    'Events',
    true,
    true,
    false,
    true,
    false,
    'Priority seating, gift bag, terrace reception — Muuqsimo London 2026.',
    'MS26-TKT-MIDNIGHT',
    '["Midnight"]'::jsonb,
    false,
    '2026-01-10 10:00:00+00'
),
(
    '66666666-6666-6666-6666-666666666666',
    'Crown Patron',
    1600.00,
    NULL,
    'https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=800&h=600&fit=crop',
    'Events',
    true,
    true,
    false,
    false,
    false,
    'Front row, founder meet-and-greet, hotel upgrade — Muuqsimo London 2026.',
    'MS26-TKT-CROWN',
    '["Midnight"]'::jsonb,
    false,
    '2026-01-10 10:00:00+00'
)
ON CONFLICT (id) DO UPDATE SET
    name          = EXCLUDED.name,
    price         = EXCLUDED.price,
    badge         = EXCLUDED.badge,
    image_url     = EXCLUDED.image_url,
    category      = EXCLUDED.category,
    is_active     = EXCLUDED.is_active,
    is_ticket     = EXCLUDED.is_ticket,
    is_featured   = EXCLUDED.is_featured,
    description   = EXCLUDED.description,
    sku           = EXCLUDED.sku,
    color_options = EXCLUDED.color_options,
    is_deleted    = EXCLUDED.is_deleted;

-- ---------------------------------------------------------------------------
-- 4) Ticket inventory — One Size
-- ---------------------------------------------------------------------------

INSERT INTO "MuuqWear".product_size_stock (
    id,
    product_id,
    size,
    quantity,
    created_at
) VALUES
(
    'b4444444-4444-4444-4444-444444444444',
    '44444444-4444-4444-4444-444444444444',
    'One Size',
    120,
    '2026-01-10 10:00:00+00'
),
(
    'b5555555-5555-5555-5555-555555555555',
    '55555555-5555-5555-5555-555555555555',
    'One Size',
    65,
    '2026-01-10 10:00:00+00'
),
(
    'b6666666-6666-6666-6666-666666666666',
    '66666666-6666-6666-6666-666666666666',
    'One Size',
    18,
    '2026-01-10 10:00:00+00'
)
ON CONFLICT (product_id, size) DO UPDATE SET
    quantity = EXCLUDED.quantity;

COMMIT;

-- ---------------------------------------------------------------------------
-- Verification — all Muuqsimo events
-- ---------------------------------------------------------------------------

SELECT
    id,
    title,
    status,
    views,
    published_at,
    content::jsonb ->> 'slug' AS slug
FROM "MuuqWear".events
WHERE id IN (
    'e0000001-0000-4000-8000-000000000001',
    'e0000002-0000-4000-8000-000000000002',
    'e0000003-0000-4000-8000-000000000003'
)
   OR content::jsonb ->> 'slug' LIKE 'muuqsimo-%'
ORDER BY published_at DESC NULLS LAST;

SELECT p.id, p.name, p.price, p.sku, s.quantity
FROM "MuuqWear".products p
LEFT JOIN "MuuqWear".product_size_stock s ON s.product_id = p.id
WHERE p.id IN (
    '44444444-4444-4444-4444-444444444444',
    '55555555-5555-5555-5555-555555555555',
    '66666666-6666-6666-6666-666666666666'
)
ORDER BY p.price;
