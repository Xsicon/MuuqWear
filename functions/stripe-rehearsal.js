process.env.FUNCTIONS_EMULATOR = "true";
process.env.GCLOUD_PROJECT = process.env.GCLOUD_PROJECT || "muuqwear-6e2e0";
process.env.FIRESTORE_EMULATOR_HOST = process.env.FIRESTORE_EMULATOR_HOST || "127.0.0.1:8080";
process.env.FIREBASE_AUTH_EMULATOR_HOST = process.env.FIREBASE_AUTH_EMULATOR_HOST || "127.0.0.1:9099";
process.env.STRIPE_WEBHOOK_SECRET = process.env.STRIPE_WEBHOOK_SECRET || "whsec_rehearsal_step25";

const Stripe = require("stripe");
const rules = require("./checkout-rules");

const PRODUCT_ID = "366f84ba-250b-4d76-ab0f-89e36890ae58";
const SIZE = "One Size";
const stamp = Date.now();

function mockRes() {
    const res = {
        statusCode: 200,
        headers: {},
        body: "",
        status(code) {
            this.statusCode = code;
            return this;
        },
        set(key, value) {
            this.headers[key] = value;
            return this;
        },
        send(body) {
            this.body = body == null ? "" : body;
            if (this._done) this._done();
            return this;
        },
    };
    res.done = new Promise((resolve) => {
        res._done = () => resolve(res);
    });
    return res;
}

async function invoke(handler, req) {
    const res = mockRes();
    await handler(req, res);
    await res.done;
    let json = null;
    if (typeof res.body === "string" && res.body.startsWith("{")) {
        try { json = JSON.parse(res.body); } catch { json = null; }
    }
    return { status: res.statusCode, body: res.body, json };
}

function jsonReq(method, body, headers) {
    return { method, headers: headers || {}, body: body || {} };
}

async function sizeStock(db) {
    const snap = await db.collection("products").doc(PRODUCT_ID).get();
    if (!snap.exists) throw new Error("Leather Keychain is missing from the Firestore emulator");
    return Number(snap.data().sizeStock?.[SIZE] ?? 0);
}

async function order(db, id) {
    const snap = await db.collection("orders").doc(id).get();
    if (!snap.exists) throw new Error(`order ${id} missing`);
    return snap.data();
}

async function signAdmin(admin) {
    const email = `rehearsal.staff.${stamp}@example.com`;
    const password = `RehearseStaff1!${stamp}`;
    const user = await admin.auth().createUser({ email, password, emailVerified: true });
    await admin.auth().setCustomUserClaims(user.uid, { admin: true });
    const login = await fetch(
        "http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=fake",
        {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ email, password, returnSecureToken: true }),
        }
    );
    const payload = await login.json();
    if (!payload.idToken) throw new Error("auth emulator did not issue a staff token");
    return payload.idToken;
}

async function webhook(handlers, stripe, type, session) {
    const payload = JSON.stringify({
        id: `evt_${type}_${stamp}`,
        object: "event",
        type,
        data: { object: session },
    });
    const signature = stripe.webhooks.generateTestHeaderString({
        payload,
        secret: process.env.STRIPE_WEBHOOK_SECRET,
    });
    return invoke(handlers.stripeWebhook, {
        method: "POST",
        headers: { "stripe-signature": signature },
        body: payload,
        rawBody: payload,
    });
}

async function startCheckout(handlers, email) {
    return invoke(handlers.createCheckoutSession, jsonReq("POST", {
        email,
        successUrl: "http://localhost:5276/order-confirmed",
        cancelUrl: "http://localhost:5276/checkout",
        items: [{ productId: PRODUCT_ID, size: SIZE, color: "Black", quantity: 1 }],
    }));
}

async function tryCharge(stripe, sessionId) {
    const session = await stripe.checkout.sessions.retrieve(sessionId, { expand: ["payment_intent"] });
    const intent = session.payment_intent;
    const intentId = typeof intent === "string" ? intent : intent?.id;
    if (!intentId) return { session, charged: false, reason: "no_payment_intent" };
    try {
        const confirmed = await stripe.paymentIntents.confirm(intentId, {
            payment_method: "pm_card_visa",
        });
        return { session, charged: confirmed.status === "succeeded", status: confirmed.status, paymentIntent: intentId };
    } catch (error) {
        return { session, charged: false, reason: error.message, paymentIntent: intentId };
    }
}

async function paidIntent(stripe, amount) {
    const intent = await stripe.paymentIntents.create({
        amount,
        currency: "usd",
        payment_method: "pm_card_visa",
        confirm: true,
        automatic_payment_methods: { enabled: true, allow_redirects: "never" },
    });
    if (intent.status !== "succeeded") throw new Error(`test PaymentIntent status ${intent.status}`);
    return intent.id;
}

async function main() {
    const secret = process.env.STRIPE_SECRET_KEY || "";
    rules.requireTestKey(secret);
    const stripe = new Stripe(secret);
    const { _handlers: handlers, ...rest } = require("./index.js");
    if (!handlers?.createCheckoutSession) throw new Error("handlers were not exported");
    const store = require("./store");
    const db = store.db;
    const report = { success: false, failure: false, expiry: false, refund: false, notes: [] };

    const startStock = await sizeStock(db);
    if (startStock < 3) throw new Error(`need 3 in stock, have ${startStock}`);

    const created = await startCheckout(handlers, `rehearsal.success.${stamp}@example.com`);
    if (created.status !== 200 || !created.json?.orderId) {
        throw new Error(`createCheckoutSession failed: ${created.status} ${created.json?.error || created.body}`);
    }
    const successId = created.json.orderId;
    const awaiting = await order(db, successId);
    if (awaiting.paymentStatus !== rules.PaymentStates.AwaitingPayment) {
        throw new Error(`expected awaiting_payment, got ${awaiting.paymentStatus}`);
    }
    const charge = await tryCharge(stripe, awaiting.checkoutSessionId);
    let paymentIntent = charge.paymentIntent;
    if (!charge.charged) {
        report.notes.push("checkout_session_confirm_skipped");
        paymentIntent = await paidIntent(stripe, awaiting.total);
    }
    const paidHook = await webhook(handlers, stripe, "checkout.session.completed", {
        id: awaiting.checkoutSessionId,
        object: "checkout.session",
        client_reference_id: successId,
        metadata: { orderId: successId },
        payment_intent: paymentIntent,
    });
    if (paidHook.status !== 200) throw new Error(`paid webhook ${paidHook.status} ${paidHook.body}`);
    const paid = await order(db, successId);
    if (paid.paymentStatus !== rules.PaymentStates.Paid) throw new Error(`paid status ${paid.paymentStatus}`);
    if (paid.paymentReference !== paymentIntent) throw new Error("webhook did not store payment_intent");
    report.success = true;

    const failedCreate = await startCheckout(handlers, `rehearsal.fail.${stamp}@example.com`);
    const failId = failedCreate.json.orderId;
    const failedHook = await webhook(handlers, stripe, "checkout.session.async_payment_failed", {
        id: (await order(db, failId)).checkoutSessionId,
        object: "checkout.session",
        client_reference_id: failId,
        metadata: { orderId: failId },
    });
    if (failedHook.status !== 200) throw new Error(`fail webhook ${failedHook.status}`);
    const failed = await order(db, failId);
    if (failed.paymentStatus !== rules.PaymentStates.PaymentFailed) throw new Error(`fail status ${failed.paymentStatus}`);
    if (failed.stockHeld) throw new Error("failed order still holds stock");
    report.failure = true;

    const expireCreate = await startCheckout(handlers, `rehearsal.expire.${stamp}@example.com`);
    const expireId = expireCreate.json.orderId;
    const expireDoc = await order(db, expireId);
    try {
        await stripe.checkout.sessions.expire(expireDoc.checkoutSessionId);
        report.notes.push("stripe_session_expired");
    } catch (error) {
        report.notes.push("stripe_session_expire_skipped");
    }
    const expireHook = await webhook(handlers, stripe, "checkout.session.expired", {
        id: expireDoc.checkoutSessionId,
        object: "checkout.session",
        client_reference_id: expireId,
        metadata: { orderId: expireId },
    });
    if (expireHook.status !== 200) throw new Error(`expire webhook ${expireHook.status}`);
    const expired = await order(db, expireId);
    if (expired.paymentStatus !== rules.PaymentStates.Expired) throw new Error(`expire status ${expired.paymentStatus}`);
    if (expired.stockHeld) throw new Error("expired order still holds stock");
    report.expiry = true;

    const staffToken = await signAdmin(store.admin);
    const refunded = await invoke(handlers.refundOrder, jsonReq("POST", {
        orderId: successId,
        reason: "Rehearsal refund",
    }, { authorization: `Bearer ${staffToken}` }));
    if (refunded.status !== 200) throw new Error(`refundOrder ${refunded.status} ${refunded.json?.error || refunded.body}`);
    const afterRefund = await order(db, successId);
    if (afterRefund.paymentStatus !== rules.PaymentStates.Refunded) throw new Error(`refund status ${afterRefund.paymentStatus}`);
    report.refund = true;

    const endStock = await sizeStock(db);
    if (endStock !== startStock) throw new Error(`stock ${startStock} -> ${endStock}`);

    const audits = await db.collection("audit").where("targetId", "==", successId).limit(1).get();
    if (audits.empty) throw new Error("refund audit missing");

    const unused = rest;
    void unused;
    const pass = report.success && report.failure && report.expiry && report.refund;
    console.log(JSON.stringify({
        pass,
        success: report.success,
        failure: report.failure,
        expiry: report.expiry,
        refund: report.refund,
        stock: endStock,
        notes: report.notes,
    }));
    if (!pass) process.exit(1);
}

main().catch((error) => {
    console.error(error.message || error);
    process.exit(1);
});
