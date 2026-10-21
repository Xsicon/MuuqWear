const fs = require("fs");
const path = require("path");
const admin = require("firebase-admin");
const rollback = require("./rollback-log");

if (!admin.apps.length) {
    admin.initializeApp({ projectId: process.env.GOOGLE_CLOUD_PROJECT || "muuqwear-6e2e0" });
}

const apply = process.argv.includes("--apply");

(async () => {
    const snap = await admin.firestore().collection("rollbackExportLog").get();
    const orders = snap.docs.map((doc) => {
        const data = doc.data() || {};
        return rollback.entry(doc.id, data.order || data, data.event || data.paymentStatus);
    });
    const payload = {
        windowId: rollback.WINDOW_ID,
        apply,
        note: apply
            ? "Apply is a dump only. It does not write Supabase. Re-enter these rows on the restored Doha site after traffic is on muuqwear-web-00004-jfc."
            : "Dry run. Pass --apply to write the JSON file.",
        rollbackTrafficCommand: "gcloud run services update-traffic muuqwear-web --project=muuqwear-6e2e0 --region=me-central1 --to-revisions=muuqwear-web-00004-jfc=100",
        orders,
    };
    const out = path.join(
        "C:/Users/lappybooks/MurabacApps/MuuqWear-archives/cutover-final-20261021-shaped",
        "rollback-reenter.json"
    );
    fs.writeFileSync(out, JSON.stringify(payload, null, 2));
    console.log(JSON.stringify({ count: orders.length, file: out, apply }, null, 2));
    process.exit(0);
})().catch((error) => {
    console.error(error.message || error);
    process.exit(1);
});
