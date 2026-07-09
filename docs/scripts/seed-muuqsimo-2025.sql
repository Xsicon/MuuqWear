-- =============================================================================
-- MUUQWEAR — Muuqsimo 2025 sample data seed
--
-- Source: MuuqWear.Web/Components/Pages/MuuqSimoComponent/MuuqSimoComponent.razor
--         (static @code data + ticket product GUIDs)
--
-- Target schema: "MuuqWear" (Supabase — same as MuuqWearApi)
-- Tables used:
--   • events              — admin Content → Events tab
--   • products            — ticket tiers (is_ticket = true)
--   • product_size_stock  — One Size inventory per ticket
--
-- HOW TO RUN
--   Supabase Dashboard → SQL → New query → paste → Run
--
-- IDEMPOTENT
--   Uses fixed UUIDs + ON CONFLICT DO UPDATE
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 1) Muuqsimo 2025 — Events content row (admin /admin/content?view=events)
--    Full page payload stored as JSON in `content` for future dynamic rendering.
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
    'e0000001-0000-4000-8000-000000000001',
    'Muuqsimo 2025',
    $json${
  "slug": "muuqsimo-2025",
  "tagline": "Where Fashion Meets Legacy",
  "eyebrow": "The Annual Flagship Gala",
  "subtitle": "The Veil Has Risen",
  "startDate": "2025-09-05T18:00:00-04:00",
  "endDate": "2025-09-07T23:59:59-04:00",
  "countdownUtc": "2025-09-05T22:00:00Z",
  "venue": {
    "name": "The Shed",
    "address": "545 West 30th Street, New York, NY 10001",
    "accessibility": "Fully ADA compliant",
    "transit": "7 Train to Hudson Yards",
    "parking": "Valet available"
  },
  "dressCode": {
    "theme": "Sapphire Veil Elegance",
    "description": "Formal attire with blue, silver, or black accents. Guests wearing Muuqwear receive a complimentary welcome gift."
  },
  "heroSlides": [
    {
      "imageUrl": "https://images.unsplash.com/photo-1700936655679-83f4b37d7d74?w=1400&h=800&fit=crop",
      "alt": "Muuqsimo Gala 1"
    },
    {
      "imageUrl": "https://images.unsplash.com/photo-1574320567434-08788f5cc928?w=1400&h=800&fit=crop",
      "alt": "Muuqsimo Gala 2"
    },
    {
      "imageUrl": "https://images.unsplash.com/photo-1724183555880-f59c7d37dac4?w=1400&h=800&fit=crop",
      "alt": "Muuqsimo Gala 3"
    }
  ],
  "experience": {
    "eyebrow": "The Experience",
    "title": "A Night Where the Brand's Most Loyal Converge",
    "body": "Muuqsimo is more than an event — it's a pilgrimage for the Muuqwear community. Top affiliates, industry press, and fashion elite gather under sapphire-lit vaults to celebrate the year's achievements and witness the unveiling of tomorrow's collection.",
    "cards": [
      {
        "title": "The Runway",
        "imageUrl": "https://images.unsplash.com/photo-1574320567434-08788f5cc928?w=600&h=400&fit=crop"
      },
      {
        "title": "The Reception",
        "imageUrl": "https://images.unsplash.com/photo-1767050362923-79ff3b7971dc?w=600&h=400&fit=crop"
      },
      {
        "title": "The Awards",
        "imageUrl": "https://images.unsplash.com/photo-1632679090212-612ac1f4d76f?w=600&h=400&fit=crop"
      },
      {
        "title": "The After-Party",
        "imageUrl": "https://images.unsplash.com/photo-1750763537578-6e592eabf730?w=600&h=400&fit=crop"
      }
    ]
  },
  "blueVeilWalk": {
    "eyebrow": "The Arrival",
    "title": "The Blue Veil Walk",
    "body": "Every guest walks a 50-foot royal blue velvet runner, lined with soft ambient lighting, as professional photographers capture the moment. This is not just an entrance — it's a statement. An orchestral soundscape swells as you cross the threshold into Muuqsimo.",
    "imageUrl": "https://images.unsplash.com/photo-1724183555880-f59c7d37dac4?w=600&h=800&fit=crop",
    "specs": [
      { "label": "Material", "value": "Plush Sapphire Velvet" },
      { "label": "Length", "value": "50 Feet" },
      { "label": "Photography", "value": "Getty Images Exclusive" },
      { "label": "Gallery", "value": "Private Upload Within 48h" }
    ]
  },
  "schedule": [
    {
      "icon": "sparkles",
      "title": "Blue Veil Arrivals",
      "time": "6:00 PM",
      "location": "Entrance Hall",
      "description": "Walk the iconic 50-foot royal blue velvet runner as photographers capture your arrival."
    },
    {
      "icon": "shirt",
      "title": "Runway Show: \"The Sapphire Prophecy\"",
      "time": "7:00 PM",
      "location": "Main Hall",
      "description": "25 never-before-seen looks unveiled across four breathtaking collections."
    },
    {
      "icon": "trophy",
      "title": "Awards Ceremony",
      "time": "8:30 PM",
      "location": "Main Hall",
      "description": "Celebrating the year's top affiliates, designers, and community champions."
    },
    {
      "icon": "wine",
      "title": "Cocktail Reception",
      "time": "9:30 PM",
      "location": "Sky Lounge",
      "description": "Open bar, hors d'oeuvres, and networking with the Muuqwear team high above the city."
    },
    {
      "icon": "music",
      "title": "After-Party (Invite Only)",
      "time": "11:00 PM",
      "location": "Underground Studio",
      "description": "An exclusive late-night celebration with live music and a world-class DJ set."
    }
  ],
  "runwayShow": {
    "eyebrow": "The Runway Show",
    "title": "The Sapphire Prophecy",
    "subtitle": "25 never-before-seen looks across four collections, presented by an international cast of diverse models.",
    "collections": [
      {
        "number": "01",
        "title": "The Essentials Collection",
        "description": "Core basics elevated with luxurious cuts and materials."
      },
      {
        "number": "02",
        "title": "The Technical Series",
        "description": "Performance wear with a luxury finish for the modern voyager."
      },
      {
        "number": "03",
        "title": "The Veil Capsule",
        "description": "Limited edition sapphire-dyed pieces — the crown jewels of the collection."
      },
      {
        "number": "04",
        "title": "The Collaboration Drop",
        "description": "A surprise guest designer unveils an exclusive co-created line."
      }
    ],
    "featuredModels": [
      { "initials": "AO", "name": "Amara Okonkwo", "credit": "Face of Muuqwear SS24" },
      { "initials": "LP", "name": "Liam Park", "credit": "International Runway (Balmain, Off-White)" },
      { "initials": "ZA", "name": "Zara Ahmed", "credit": "Vogue Arabia Cover Star" },
      { "initials": "CR", "name": "Caleb Rivera", "credit": "Muuqwear Discovery Program" }
    ],
    "modelsExtra": "+ 20 additional diverse models",
    "musicLighting": [
      {
        "icon": "music",
        "title": "Live Orchestra",
        "description": "String quartet for the first half; electronic ambient for the second."
      },
      {
        "icon": "sparkles",
        "title": "Dynamic Lighting",
        "description": "Sapphire and silver lighting that shifts with each collection segment."
      },
      {
        "icon": "play",
        "title": "Walk Style",
        "description": "Slow, deliberate, geometric pacing — emphasizing garment structure."
      }
    ],
    "production": [
      { "role": "Sound", "provider": "LMG Productions" },
      { "role": "Venue", "provider": "The Shed" },
      { "role": "Catering", "provider": "Union Square Events" },
      { "role": "Photo", "provider": "Getty Images" }
    ]
  },
  "awards": [
    { "icon": "gem", "title": "Best New Innovation Outfit", "prize": "$10,000 + Featured in Next Campaign" },
    { "icon": "crown", "title": "Affiliate of the Year (Gold)", "prize": "$25,000 + Paris Fashion Week Trip" },
    { "icon": "award", "title": "Affiliate of the Year (Silver)", "prize": "$10,000 + Luxury Gift Package" },
    { "icon": "trophy", "title": "Affiliate of the Year (Bronze)", "prize": "$5,000 + Muuqwear Wardrobe" },
    { "icon": "star", "title": "Rising Star Award", "prize": "$2,500 + Founder Mentorship" },
    { "icon": "users", "title": "Community Choice Award", "prize": "$5,000 + Custom Design Collab" },
    { "icon": "shield", "title": "Lifetime Achievement", "prize": "Custom Trophy + Lifetime VIP" },
    { "icon": "camera", "title": "Best Styling (Social Media)", "prize": "$1,500 + Homepage Feature" }
  ],
  "tickets": {
    "capacityNote": "Limited to 500 Attendees",
    "subtitle": "All tickets include runway show, awards ceremony & cocktail reception.",
    "tierConfigs": [
      {
        "productId": "11111111-1111-1111-1111-111111111111",
        "totalCapacity": 250,
        "perks": ["Standard admission", "Open bar & hors d'oeuvres", "Runway show & awards ceremony"],
        "isHighlighted": false,
        "badge": null
      },
      {
        "productId": "22222222-2222-2222-2222-222222222222",
        "totalCapacity": 150,
        "perks": ["Priority front-section seating", "Welcome gift bag", "After-party access", "All Veil Access perks"],
        "isHighlighted": true,
        "badge": "Most Popular"
      },
      {
        "productId": "33333333-3333-3333-3333-333333333333",
        "totalCapacity": 75,
        "perks": ["Front row seating", "VIP lounge access", "Meet-and-greet with designers", "After-party VIP section", "Luxury hotel room upgrade", "All Sapphire Circle perks"],
        "isHighlighted": false,
        "badge": null
      }
    ],
    "affiliateBenefits": [
      { "tier": "Bronze", "benefit": "25% off Veil Access ticket", "dotColor": "#CD7F32" },
      { "tier": "Silver", "benefit": "Complimentary Sapphire Circle ticket", "dotColor": "#A9B7CC" },
      { "tier": "Gold", "benefit": "All-expenses-paid VIP trip (flights, luxury hotel, VIP access)", "dotColor": "#D4A843" }
    ],
    "pressContact": "press@muuqwear.com"
  },
  "team": [
    { "name": "Jordan Muuq", "role": "Founder & Creative Director" },
    { "name": "Elena Veil", "role": "Co-Founder & Creative Director" },
    { "name": "Marcus Chen", "role": "Head of Design" },
    { "name": "Sophia Laurent", "role": "Event Director" },
    { "name": "David Okafor", "role": "Affiliate Program Manager" },
    { "name": "Kaito Tanaka", "role": "Runway Producer" }
  ],
  "sponsors": [
    { "role": "Presenting Sponsor", "name": "Maison Luxe" },
    { "role": "Runway Sponsor", "name": "Atelier Collective" },
    { "role": "Hospitality Sponsor", "name": "The Sapphire Group" },
    { "role": "Gift Bag Sponsor", "name": "Noir Beauty" }
  ],
  "closingCta": {
    "eyebrow": "New York City · September 2025",
    "title": "The Veil Rises",
    "imageUrl": "https://images.unsplash.com/photo-1619439443981-2f7607f6c346?w=1400&h=800&fit=crop"
  }
}$json$,
    'published',
    2847,
    '2025-03-01 12:00:00+00',
    '2025-03-15 09:00:00+00'
)
ON CONFLICT (id) DO UPDATE SET
    title        = EXCLUDED.title,
    content      = EXCLUDED.content,
    status       = EXCLUDED.status,
    views        = EXCLUDED.views,
    published_at = EXCLUDED.published_at;

-- ---------------------------------------------------------------------------
-- 2) Ticket products — GUIDs must match MuuqSimoComponent.razor TicketTiers
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
    '11111111-1111-1111-1111-111111111111',
    'Veil Access',
    250.00,
    NULL,
    'https://images.unsplash.com/photo-1700936655679-83f4b37d7d74?w=800&h=600&fit=crop',
    'Events',
    true,
    true,
    false,
    false,
    false,
    'Muuqsimo 2025 standard admission. Includes open bar, hors d''oeuvres, runway show, and awards ceremony.',
    'MS-TKT-VEIL',
    '["Sapphire"]'::jsonb,
    false,
    '2025-03-01 12:00:00+00'
),
(
    '22222222-2222-2222-2222-222222222222',
    'Sapphire Circle',
    500.00,
    'Most Popular',
    'https://images.unsplash.com/photo-1574320567434-08788f5cc928?w=800&h=600&fit=crop',
    'Events',
    true,
    true,
    false,
    true,
    false,
    'Priority front-section seating, welcome gift bag, after-party access, and all Veil Access perks.',
    'MS-TKT-SAPPHIRE',
    '["Sapphire"]'::jsonb,
    false,
    '2025-03-01 12:00:00+00'
),
(
    '33333333-3333-3333-3333-333333333333',
    'Legacy Patron',
    1500.00,
    NULL,
    'https://images.unsplash.com/photo-1724183555880-f59c7d37dac4?w=800&h=600&fit=crop',
    'Events',
    true,
    true,
    false,
    false,
    false,
    'Front row seating, VIP lounge, designer meet-and-greet, after-party VIP section, luxury hotel upgrade, and all Sapphire Circle perks.',
    'MS-TKT-LEGACY',
    '["Sapphire"]'::jsonb,
    false,
    '2025-03-01 12:00:00+00'
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
-- 3) Ticket inventory — One Size (matches cart add on /muuqsimo)
--    Quantities = remaining counts shown on the static page.
--    Upserts on (product_id, size) — rows may already exist with different ids.
-- ---------------------------------------------------------------------------

INSERT INTO "MuuqWear".product_size_stock (
    id,
    product_id,
    size,
    quantity,
    created_at
) VALUES
(
    'b1111111-1111-1111-1111-111111111111',
    '11111111-1111-1111-1111-111111111111',
    'One Size',
    87,
    '2025-03-01 12:00:00+00'
),
(
    'b2222222-2222-2222-2222-222222222222',
    '22222222-2222-2222-2222-222222222222',
    'One Size',
    42,
    '2025-03-01 12:00:00+00'
),
(
    'b3333333-3333-3333-3333-333333333333',
    '33333333-3333-3333-3333-333333333333',
    'One Size',
    11,
    '2025-03-01 12:00:00+00'
)
ON CONFLICT (product_id, size) DO UPDATE SET
    quantity = EXCLUDED.quantity;

COMMIT;

-- ---------------------------------------------------------------------------
-- Verification
-- ---------------------------------------------------------------------------
SELECT id, title, status, views, published_at
FROM "MuuqWear".events
WHERE id = 'e0000001-0000-4000-8000-000000000001';

SELECT p.id, p.name, p.price, p.badge, p.is_ticket, s.size, s.quantity
FROM "MuuqWear".products p
LEFT JOIN "MuuqWear".product_size_stock s ON s.product_id = p.id
WHERE p.id IN (
    '11111111-1111-1111-1111-111111111111',
    '22222222-2222-2222-2222-222222222222',
    '33333333-3333-3333-3333-333333333333'
)
ORDER BY p.price;
