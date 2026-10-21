const WINDOW_ID = "rollback-20261021";

function windowId() {
    return process.env.ROLLBACK_WINDOW_ID || WINDOW_ID;
}

function entry(orderId, data, event) {
    const order = data || {};
    return {
        windowId: windowId(),
        orderId: String(orderId || ""),
        event: String(event || "snapshot"),
        source: "firebase",
        email: order.email || null,
        customerId: order.customerId || null,
        paymentStatus: order.paymentStatus || null,
        total: Number(order.total || 0),
        checkoutSessionId: order.checkoutSessionId || null,
        paymentReference: order.paymentReference || null,
        refundId: order.refundId || null,
        items: Array.isArray(order.items) ? order.items : [],
        loggedAt: new Date().toISOString(),
    };
}

module.exports = { WINDOW_ID, windowId, entry };
