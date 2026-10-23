/**
 * Read-only import of MuuqWear company pages from shared Supabase into Firestore.
 * Never deletes Supabase rows. Upserts Firestore docs by existing Guid ids.
 *
 * Env:
 *   SUPABASE_URL
 *   SUPABASE_SERVICE_ROLE_KEY or SUPABASE_ANON_KEY
 *   GOOGLE_CLOUD_PROJECT / GCLOUD_PROJECT (default muuqwear-6e2e0)
 */
import { readFile } from "node:fs/promises";
import { existsSync } from "node:fs";
import path from "node:path";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const require = createRequire(path.join(scriptDir, "../../functions/node_modules/firebase-admin/package.json"));
const admin = require("firebase-admin");

const PROJECT_ID = process.env.GOOGLE_CLOUD_PROJECT || process.env.GCLOUD_PROJECT || "muuqwear-6e2e0";
const SUPABASE_URL = (process.env.SUPABASE_URL || "https://yvooccmbtokzfnqibyig.supabase.co").replace(/\/$/, "");
const SUPABASE_KEY = process.env.SUPABASE_SERVICE_ROLE_KEY || process.env.SUPABASE_ANON_KEY;
const SCHEMA = "MuuqWear";
const DUMP_DIR = process.env.COMPANY_PAGES_DUMP_DIR ||
  "C:\\Users\\lappybooks\\MurabacApps\\MuuqWear-archives\\cutover-final-20261021\\database\\postgres\\MuuqWear";

const MUUQSIMO_2025_TIERS = [
  {
    productId: "11111111-1111-1111-1111-111111111111",
    totalCapacity: 250,
    perks: ["General admission", "Runway show access", "Awards ceremony", "Cocktail reception"],
    isHighlighted: false,
    badge: null,
  },
  {
    productId: "22222222-2222-2222-2222-222222222222",
    totalCapacity: 180,
    perks: ["Priority seating", "Welcome gift", "All Veil Access perks"],
    isHighlighted: true,
    badge: "Most Popular",
  },
  {
    productId: "33333333-3333-3333-3333-333333333333",
    totalCapacity: 70,
    perks: ["Front row seating", "VIP lounge access", "Meet-and-greet with designers", "After-party VIP section", "Luxury hotel room upgrade", "All Sapphire Circle perks"],
    isHighlighted: false,
    badge: null,
  },
];

if (!admin.apps.length) {
  admin.initializeApp({ projectId: PROJECT_ID });
}

const db = admin.firestore();

function pick(row, ...names) {
  for (const name of names) {
    if (row[name] != null && row[name] !== "") return row[name];
  }
  return null;
}

function asDate(value) {
  if (!value) return null;
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

async function fetchTable(table) {
  if (SUPABASE_KEY) {
    const url = `${SUPABASE_URL}/rest/v1/${table}?select=*`;
    const response = await fetch(url, {
      headers: {
        apikey: SUPABASE_KEY,
        Authorization: `Bearer ${SUPABASE_KEY}`,
        "Accept-Profile": SCHEMA,
        "Content-Profile": SCHEMA,
        Prefer: "count=exact",
      },
    });
    if (!response.ok) {
      const body = await response.text();
      throw new Error(`${table} HTTP ${response.status}: ${body.slice(0, 300)}`);
    }
    console.log(`read ${table} from supabase`);
    return response.json();
  }

  const dumpPath = path.join(DUMP_DIR, `${table}.json`);
  if (!existsSync(dumpPath)) {
    throw new Error(`No Supabase key and dump missing: ${dumpPath}`);
  }
  console.log(`read ${table} from dump`);
  return JSON.parse(await readFile(dumpPath, "utf8"));
}

function ensureEventBody(raw, title) {
  let parsed;
  try {
    parsed = typeof raw === "string" ? JSON.parse(raw) : { ...(raw || {}) };
  } catch {
    parsed = {};
  }
  parsed.tickets ??= {};
  if (!Array.isArray(parsed.tickets.tierConfigs) || parsed.tickets.tierConfigs.length === 0) {
    if (parsed.slug === "muuqsimo-2025" || /2025/i.test(title || "")) {
      parsed.tickets.tierConfigs = MUUQSIMO_2025_TIERS;
    }
  }
  return JSON.stringify(parsed);
}

function contentFields(item) {
  const fields = {
    title: item.title || "",
    body: item.body || "",
    status: item.status || "draft",
    category: item.category || "",
    imageUrl: item.imageUrl || "",
    secondImageUrl: item.secondImageUrl || "",
    author: item.author || "",
    excerpt: item.excerpt || "",
    slug: item.slug || "",
    designer: item.designer || "",
    year: item.year || "",
    collection: item.collection || "",
    inspiration: item.inspiration || "",
    technicalFabric: item.technicalFabric || "",
    technicalTechniques: item.technicalTechniques || "",
    technicalProduction: item.technicalProduction || "",
    technicalAvailability: item.technicalAvailability || "",
    views: Number(item.views || 0),
    createdAt: item.createdAt || new Date(),
  };
  if (item.productId) fields.productId = item.productId;
  if (item.publishedAt) fields.publishedAt = item.publishedAt;
  return fields;
}

async function importJobs() {
  const rows = await fetchTable("job_postings");
  let count = 0;
  for (const row of rows) {
    const id = pick(row, "id");
    if (!id) continue;
    const createdAt = asDate(pick(row, "created_at", "createdAt")) || new Date();
    const openedAt = asDate(pick(row, "opened_at", "openedAt")) || createdAt;
    const closedAt = asDate(pick(row, "closed_at", "closedAt"));
    const fields = {
      slug: pick(row, "slug") || String(id).replaceAll("-", "").slice(0, 8),
      title: pick(row, "title") || "",
      department: pick(row, "department") || "",
      location: pick(row, "location") || "",
      type: pick(row, "type") || "",
      description: pick(row, "description") || "",
      status: pick(row, "status") || "open",
      createdAt,
      openedAt,
    };
    if (closedAt) fields.closedAt = closedAt;
    await db.collection("jobPostings").doc(String(id)).set(fields, { merge: true });
    count += 1;
  }
  return { table: "job_postings", count };
}

async function importDesignHistory() {
  const rows = await fetchTable("design_history");
  let count = 0;
  for (const row of rows) {
    const id = pick(row, "id");
    if (!id) continue;
    const body = pick(row, "content", "body", "inspiration") || "";
    const slug = pick(row, "slug");
    await db.collection("content").doc(String(id)).set(contentFields({
      title: pick(row, "title") || "",
      body: typeof body === "string" ? body : JSON.stringify(body),
      status: pick(row, "status") || "draft",
      category: "DesignHistory",
      imageUrl: pick(row, "image_url", "imageUrl"),
      secondImageUrl: pick(row, "second_image_url", "secondImageUrl"),
      designer: pick(row, "designer"),
      year: pick(row, "year") != null ? String(pick(row, "year")) : "",
      collection: pick(row, "collection"),
      inspiration: pick(row, "inspiration"),
      technicalFabric: pick(row, "technical_fabric", "technicalFabric"),
      technicalTechniques: pick(row, "technical_techniques", "technicalTechniques"),
      technicalProduction: pick(row, "technical_production", "technicalProduction"),
      technicalAvailability: pick(row, "technical_availability", "technicalAvailability"),
      productId: pick(row, "product_id", "productId"),
      views: pick(row, "views") || 0,
      slug,
      createdAt: asDate(pick(row, "created_at", "createdAt")) || new Date(),
      publishedAt: asDate(pick(row, "published_at", "publishedAt")),
    }), { merge: true });
    count += 1;
  }
  return { table: "design_history", count };
}

async function importEvents() {
  const rows = await fetchTable("events");
  let count = 0;
  for (const row of rows) {
    const id = pick(row, "id");
    if (!id) continue;
    const raw = pick(row, "content", "body") || "{}";
    const body = ensureEventBody(raw, pick(row, "title"));
    let slug = pick(row, "slug");
    try {
      const parsed = typeof raw === "string" ? JSON.parse(raw) : raw;
      slug = slug || parsed?.slug;
    } catch {
      // keep existing slug
    }
    await db.collection("content").doc(String(id)).set(contentFields({
      title: pick(row, "title") || "",
      body,
      status: pick(row, "status") || "draft",
      category: "Events",
      slug,
      views: pick(row, "views") || 0,
      createdAt: asDate(pick(row, "created_at", "createdAt")) || new Date(),
      publishedAt: asDate(pick(row, "published_at", "publishedAt")),
    }), { merge: true });
    count += 1;
  }
  return { table: "events", count };
}

const results = [];
results.push(await importJobs());
results.push(await importDesignHistory());
results.push(await importEvents());
for (const result of results) {
  console.log(`${result.table}: upserted ${result.count}`);
}
process.exit(0);
