const { http } = require("@google-cloud/functions-framework");
const { onRequest } = require("firebase-functions/v2/https");
const Stripe = require("stripe");
const rules = require("./checkout-rules");
const store = require("./store");
const accountEmail = require("./account-email");

const handlers = {};
function serve(name, handler) {
    handlers[name] = handler;
    if (process.env.FUNCTIONS_EMULATOR === "true") return;
    const target = process.env.FUNCTION_TARGET || process.env.K_SERVICE;
    if (!target || target === name) http(name, handler);
}

function stripeClient() {
    const key = process.env.STRIPE_SECRET_KEY || "";
    rules.requireTestKey(key);
    return new Stripe(key);
}

function siteUrl(body) {
    const fallback = "https://muuqwear.com";
    const url = String(body?.successUrl || body?.cancelUrl || fallback);
    try {
        const host = new URL(url).hostname;
        if (host === "localhost" || host.endsWith("muuqwear.com") || host.endsWith("web.app") || host.endsWith("firebaseapp.com")) {
            return { success: body?.successUrl || `${fallback}/order-confirmed`, cancel: body?.cancelUrl || `${fallback}/checkout` };
        }
    } catch {
        // Use the live site when the client omits a return URL.
    }
    return { success: `${fallback}/order-confirmed`, cancel: `${fallback}/checkout` };
}

serve("createCheckoutSession", async (req, res) => {
    store.allowOrigin(req, res);
    if (req.method === "OPTIONS") {
        res.status(204).send("");
        return;
    }
    if (req.method !== "POST") {
        store.json(res, 405, { error: "POST only" });
        return;
    }
    if (process.env.CUTOVER_FREEZE_WRITES === "true") {
        store.json(res, 503, { error: "Checkout and account changes are paused. You can still browse." });
        return;
    }

    try {
        const stripe = stripeClient();
        const user = await store.caller(req);
        const body = req.body || {};
        let lines = store.cartLinesFromBody(body);
        if (!lines.length && user.uid) {
            const cart = await store.db.collection("customers").doc(user.uid).collection("cart").doc("current").get();
            lines = store.cartLinesFromDoc(cart.data() || {});
        }
        const products = await store.loadProducts(lines);
        const priced = rules.price(lines, products);
        const total = rules.sum(priced);
        const email = String(body.email || user.email || "").trim();
        if (!email) {
            store.json(res, 400, { error: "An email is required to pay." });
            return;
        }

        const customerKey = user.uid || `guest:${email.toLowerCase()}`;
        const key = rules.idempotencyKey(customerKey, lines);
        const existing = await store.db.collection("orders").where("idempotencyKey", "==", key).limit(1).get();
        if (!existing.empty) {
            const order = existing.docs[0].data();
            if (order.checkoutUrl && order.paymentStatus === rules.PaymentStates.AwaitingPayment) {
                store.json(res, 200, { orderId: existing.docs[0].id, url: order.checkoutUrl });
                return;
            }
        }

        const code = String(body.referralCode || "").trim();
        const referred = await store.referral(code);
        const orderRef = store.db.collection("orders").doc();
        const urls = siteUrl(body);
        const successUrl = urls.success.includes(orderRef.id)
            ? urls.success
            : `${String(urls.success).replace(/\/$/, "")}/${orderRef.id}`;

        const session = await stripe.checkout.sessions.create({
            mode: "payment",
            customer_email: email,
            client_reference_id: orderRef.id,
            success_url: successUrl,
            cancel_url: urls.cancel,
            line_items: priced.map((line) => ({
                quantity: line.quantity,
                price_data: {
                    currency: "usd",
                    unit_amount: line.priceCents,
                    product_data: { name: `${line.name} / ${line.size}` },
                },
            })),
            metadata: { orderId: orderRef.id, customerKey },
        }, { idempotencyKey: key });

        await store.db.runTransaction(async (tx) => {
            await store.holdStock(priced, tx);
            tx.set(orderRef, {
                customerId: user.uid || "guest",
                email,
                status: "pending",
                paymentStatus: rules.PaymentStates.AwaitingPayment,
                subtotal: total,
                shipping: 0,
                tax: 0,
                total,
                items: priced,
                createdAt: store.admin.firestore.FieldValue.serverTimestamp(),
                idempotencyKey: key,
                checkoutSessionId: session.id,
                checkoutUrl: session.url,
                stockHeld: true,
                referralCode: referred?.code || null,
                referralType: referred?.type || null,
                ambassadorId: referred?.ambassadorId || null,
            });
        });

        if (user.uid) {
            await store.db.collection("customers").doc(user.uid).collection("cart").doc("current").set({ items: [] });
        }

        await store.appendRollbackLog(orderRef.id, "created");
        store.json(res, 200, { orderId: orderRef.id, url: session.url });
    } catch (error) {
        store.json(res, 400, { error: error.message || "Checkout failed" });
    }
});

serve("stripeWebhook", async (req, res) => {
    if (req.method !== "POST") {
        res.status(405).send("POST only");
        return;
    }

    let event;
    try {
        const stripe = stripeClient();
        const secret = process.env.STRIPE_WEBHOOK_SECRET || "";
        const payload = req.rawBody || req.body;
        event = stripe.webhooks.constructEvent(payload, req.headers["stripe-signature"], secret);
    } catch (error) {
        res.status(400).send(`Webhook signature failed: ${error.message}`);
        return;
    }

    try {
        if (event.type === "checkout.session.completed") {
            await onPaid(event.data.object);
        } else if (event.type === "checkout.session.expired" || event.type === "checkout.session.async_payment_failed") {
            const failed = event.type !== "checkout.session.expired";
            await onOpenFailed(event.data.object, failed ? rules.PaymentStates.PaymentFailed : rules.PaymentStates.Expired);
        } else if (event.type === "charge.dispute.created") {
            await onDispute(event.data.object);
        }
        res.status(200).send("ok");
    } catch (error) {
        res.status(500).send(error.message || "webhook failed");
    }
});

serve("sendVerificationEmail", accountEmail.sendVerificationEmail);

serve("refundOrder", async (req, res) => {
    store.allowOrigin(req, res);
    if (req.method === "OPTIONS") {
        res.status(204).send("");
        return;
    }
    if (req.method !== "POST") {
        store.json(res, 405, { error: "POST only" });
        return;
    }
    if (process.env.CUTOVER_FREEZE_WRITES === "true") {
        store.json(res, 503, { error: "Checkout and account changes are paused. You can still browse." });
        return;
    }

    try {
        const user = await store.caller(req);
        if (!user.admin) {
            store.json(res, 403, { error: "Staff refunds require the admin claim." });
            return;
        }
        const body = req.body || {};
        const orderId = String(body.orderId || "");
        const reason = String(body.reason || "").trim();
        if (!orderId || !reason) {
            store.json(res, 400, { error: "orderId and reason are required." });
            return;
        }

        const orderRef = store.db.collection("orders").doc(orderId);
        const snap = await orderRef.get();
        if (!snap.exists) {
            store.json(res, 404, { error: "Order not found" });
            return;
        }
        const order = snap.data();
        if (!rules.canRefund(order.paymentStatus)) {
            store.json(res, 409, { error: "Only a paid order can be refunded." });
            return;
        }

        const stripe = stripeClient();
        const refund = await stripe.refunds.create({
            payment_intent: order.paymentReference,
            reason: "requested_by_customer",
            metadata: { orderId, actorUid: user.uid, reason },
        });

        await store.db.runTransaction(async (tx) => {
            const fresh = await tx.get(orderRef);
            const current = fresh.data();
            if (!rules.canRefund(current.paymentStatus)) return;
            if (current.stockHeld) {
                await store.releaseStock(current, tx);
            }
            tx.update(orderRef, {
                paymentStatus: rules.PaymentStates.Refunded,
                refundId: refund.id,
                refundedBy: user.uid,
                refundReason: reason,
                stockHeld: false,
            });
            tx.set(store.db.collection("audit").doc(), {
                actorUid: user.uid,
                action: "refund",
                targetId: orderId,
                at: store.admin.firestore.FieldValue.serverTimestamp(),
                reason,
            });
        });

        await store.appendRollbackLog(orderId, "refunded");
        store.json(res, 200, { orderId, refundId: refund.id });
    } catch (error) {
        store.json(res, 400, { error: error.message || "Refund failed" });
    }
});

async function onPaid(session) {
    const orderId = session.metadata?.orderId || session.client_reference_id;
    if (!orderId) return;
    const orderRef = store.db.collection("orders").doc(orderId);
    await store.db.runTransaction(async (tx) => {
        const snap = await tx.get(orderRef);
        if (!snap.exists) throw new Error(`Order ${orderId} is missing`);
        const order = snap.data();
        if (!rules.canPay(order.paymentStatus)) return;
        tx.update(orderRef, {
            paymentStatus: rules.PaymentStates.Paid,
            paymentReference: session.payment_intent || session.id,
            paidAt: store.admin.firestore.FieldValue.serverTimestamp(),
        });
        const amount = rules.commissionAmount(order.referralType, Number(order.total || 0));
        if (amount && order.ambassadorId) {
            tx.set(store.db.collection("commissions").doc(), {
                ambassadorId: order.ambassadorId,
                orderId,
                amount,
                orderTotal: Number(order.total || 0),
                rate: 0.1,
                status: "pending",
                createdAt: store.admin.firestore.FieldValue.serverTimestamp(),
            });
        }
    });
    await store.appendRollbackLog(orderId, "paid");
}

async function onOpenFailed(session, paymentStatus) {
    const orderId = session.metadata?.orderId || session.client_reference_id;
    if (!orderId) return;
    const orderRef = store.db.collection("orders").doc(orderId);
    await store.db.runTransaction(async (tx) => {
        const snap = await tx.get(orderRef);
        if (!snap.exists) return;
        const order = snap.data();
        const allowed = paymentStatus === rules.PaymentStates.Expired ? rules.canExpire(order.paymentStatus) : rules.canFail(order.paymentStatus);
        if (!allowed) return;
        if (order.stockHeld) await store.releaseStock(order, tx);
        tx.update(orderRef, { paymentStatus, stockHeld: false });
    });
    const snap = await orderRef.get();
    if (snap.exists) await store.restoreCart(snap.data());
    await store.appendRollbackLog(orderId, paymentStatus);
}

async function onDispute(dispute) {
    const paymentIntent = typeof dispute.payment_intent === "string" ? dispute.payment_intent : dispute.payment_intent?.id;
    if (!paymentIntent) return;
    const found = await store.db.collection("orders").where("paymentReference", "==", paymentIntent).limit(1).get();
    if (found.empty) return;
    await found.docs[0].ref.update({ dispute: true, disputeId: dispute.id });
    await store.appendRollbackLog(found.docs[0].id, "dispute");
}

const httpsOptions = { region: "europe-west2" };
exports.createCheckoutSession = onRequest(httpsOptions, handlers.createCheckoutSession);
exports.stripeWebhook = onRequest(httpsOptions, handlers.stripeWebhook);
exports.refundOrder = onRequest(httpsOptions, handlers.refundOrder);
exports.sendVerificationEmail = onRequest(httpsOptions, handlers.sendVerificationEmail);
exports._handlers = handlers;
