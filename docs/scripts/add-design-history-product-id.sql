-- Links design history stories to storefront products for "Shop This Design".
-- REQUIRED: run this migration before deploying API changes that read/write product_id.
-- Run in Supabase SQL editor against the MuuqWear schema.

ALTER TABLE "MuuqWear".design_history
    ADD COLUMN IF NOT EXISTS product_id uuid NULL
        REFERENCES "MuuqWear".products(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS idx_design_history_product_id
    ON "MuuqWear".design_history (product_id);
