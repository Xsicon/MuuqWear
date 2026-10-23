const admin = require("firebase-admin");
const rules = require("./checkout-rules");
const rollback = require("./rollback-log");

if (!admin.apps.length) {
    admin.initializeApp();
}

const db = admin.firestore();

function json(res, status, body) {
    res.status(status).set("content-type", "application/json").send(JSON.stringify(body));
}

function allowOrigin(req, res) {
    const origin = req.headers.origin || "";
    const allowed = /^(https:\/\/(www\.)?muuqwear\.com|https:\/\/muuqwear-6e2e0\.(web|firebaseapp)\.app|http:\/\/localhost:(5276|5000))$/.test(origin);
    if (allowed) {
        res.set("access-control-allow-origin", origin);
        res.set("vary", "origin");
    }
    res.set("access-control-allow-headers", "authorization,content-type");
    res.set("access-control-allow-methods", "POST,OPTIONS");
}

async function caller(req) {
    const header = req.headers.authorization || "";
    const token = header.startsWith("Bearer ") ? header.slice(7) : "";
    if (!token) return { uid: null, admin: false, email: null };
    const decoded = await admin.auth().verifyIdToken(token);
    return { uid: decoded.uid, admin: decoded.admin === true, email: decoded.email || null };
}

function cartLinesFromBody(body) {
    const items = Array.isArray(body?.items) ? body.items : [];
    return items.map((item) => {
        rules.rejectClientPrices(item);
        return {
            productId: String(item.productId || ""),
            size: String(item.size || ""),
            color: String(item.color || ""),
            quantity: Number(item.quantity || 0),
        };
    }).filter((line) => line.productId);
}

function cartLinesFromDoc(data) {
    const items = Array.isArray(data?.items) ? data.items : [];
    return items.map((item) => ({
        productId: String(item.productId || item.id || ""),
        size: String(item.size || ""),
        color: String(item.color || ""),
        quantity: Number(item.quantity || 0),
    })).filter((line) => line.productId);
}

async function loadProducts(lines) {
    const quotes = {};
    for (const line of lines) {
        if (quotes[line.productId]) continue;
        const snap = await db.collection("products").doc(line.productId).get();
        if (!snap.exists) continue;
        const data = snap.data() || {};
        const sizeStock = {};
        const raw = data.sizeStock || {};
        for (const [size, quantity] of Object.entries(raw)) sizeStock[size] = Number(quantity || 0);
        quotes[line.productId] = {
            id: snap.id,
            name: data.name || snap.id,
            status: data.status || "",
            price: Number(data.price || 0),
            sizeStock,
        };
    }
    return quotes;
}

async function referral(code) {
    if (!code) return null;
    const snap = await db.collection("referralCodes").doc(code).get();
    if (!snap.exists) return null;
    const data = snap.data() || {};
    if (data.status !== "active") return null;
    const owners = await db.collection("customers").where("referralCode", "==", code).limit(1).get();
    const ambassadorId = owners.empty ? null : owners.docs[0].id;
    return { code, type: data.type || "", ambassadorId };
}

async function restoreCart(order) {
    if (!order.customerId || order.customerId === "guest") return;
    const ref = db.collection("customers").doc(order.customerId).collection("cart").doc("current");
    await ref.set({ items: order.items || [] }, { merge: true });
}

async function applyStockMoves(lines, tx, direction, { requireProduct = false } = {}) {
    const byProduct = new Map();
    for (const line of lines || []) {
        const id = String(line.productId || "");
        if (!id) continue;
        if (!byProduct.has(id)) byProduct.set(id, []);
        byProduct.get(id).push(line);
    }

    const ids = [...byProduct.keys()];
    const refs = ids.map((id) => db.collection("products").doc(id));
    const snaps = refs.length ? await tx.getAll(...refs) : [];

    snaps.forEach((snap, index) => {
        const ref = refs[index];
        if (!snap.exists) {
            if (requireProduct) throw new Error(`Unknown product ${ref.id}.`);
            return;
        }
        const data = snap.data() || {};
        let sizeStock = { ...(data.sizeStock || {}) };
        for (const line of byProduct.get(ref.id) || []) {
            sizeStock = rules.moveStock(sizeStock, line.size, Number(line.quantity || 0), direction);
        }
        tx.update(ref, { sizeStock });
    });
}

async function releaseStock(order, tx) {
    await applyStockMoves(order.items || [], tx, 1);
}

async function appendRollbackLog(orderId, event) {
    const id = String(orderId || "");
    if (!id) return;
    const snap = await db.collection("orders").doc(id).get();
    if (!snap.exists) return;
    const row = rollback.entry(id, snap.data(), event);
    await db.collection("rollbackExportLog").doc(id).set({
        ...row,
        order: snap.data(),
        at: admin.firestore.FieldValue.serverTimestamp(),
    }, { merge: true });
}

async function holdStock(priced, tx) {
    await applyStockMoves(priced, tx, -1, { requireProduct: true });
}

module.exports = {
    admin,
    db,
    json,
    allowOrigin,
    caller,
    cartLinesFromBody,
    cartLinesFromDoc,
    loadProducts,
    referral,
    restoreCart,
    releaseStock,
    holdStock,
    appendRollbackLog,
};
