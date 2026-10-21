const admin = require("firebase-admin");

process.env.GCLOUD_PROJECT = process.env.GCLOUD_PROJECT || "muuqwear-6e2e0";
process.env.FIRESTORE_EMULATOR_HOST = process.env.FIRESTORE_EMULATOR_HOST || "127.0.0.1:8080";
process.env.FIREBASE_AUTH_EMULATOR_HOST = process.env.FIREBASE_AUTH_EMULATOR_HOST || "127.0.0.1:9099";

if (!admin.apps.length) {
    admin.initializeApp({ projectId: "muuqwear-6e2e0" });
}

const db = admin.firestore();
const seedId = "emu-oxford";

async function rest(method, path, body) {
    const url = `http://127.0.0.1:8080/v1/projects/muuqwear-6e2e0/databases/(default)/documents${path}`;
    const response = await fetch(url, {
        method,
        headers: body ? { "content-type": "application/json" } : undefined,
        body: body ? JSON.stringify(body) : undefined,
    });
    return { status: response.status, text: await response.text() };
}

async function main() {
    await db.collection("products").doc(seedId).set({
        name: "Emulator Oxford",
        slug: "emu-oxford",
        status: "published",
        price: 7500,
        sizeStock: { M: 3 },
        source: "emulator-only",
    });
    const product = await db.collection("products").doc(seedId).get();
    if (!product.exists) throw new Error("emulator seed was not written");
    if (product.data().source !== "emulator-only") throw new Error("refused to use production data");

    const email = "emulator.shopper@example.com";
    let user;
    try {
        user = await admin.auth().getUserByEmail(email);
    } catch {
        user = await admin.auth().createUser({
            email,
            password: "TempPass1!",
            emailVerified: true,
        });
    }
    if (!user.uid) throw new Error("auth emulator did not create a user");

    const denied = await rest("POST", "/products", {
        fields: { status: { stringValue: "published" }, name: { stringValue: "hack" } },
    });
    if (denied.status < 400) throw new Error("closed rules allowed an anonymous product write");

    const functionsUrl = "http://127.0.0.1:5001/muuqwear-6e2e0/europe-west2/createCheckoutSession";
    const fn = await fetch(functionsUrl, { method: "GET" });
    if (fn.status === 404) throw new Error("functions emulator did not load createCheckoutSession");

    const bucket = admin.storage().bucket("muuqwear-6e2e0.firebasestorage.app");
    const file = bucket.file("site/emu-probe.txt");
    await file.save("emulator-only", { contentType: "text/plain" });
    const [exists] = await file.exists();
    if (!exists) throw new Error("storage emulator did not accept a probe object");
    await file.delete();

    console.log("emulator_ok products=1 auth_user=1 anonymous_write=" + denied.status + " functions=" + fn.status + " storage=1");
}

main().catch((error) => {
    console.error(error.message || error);
    process.exit(1);
});
