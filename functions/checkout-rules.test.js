const test = require("node:test");
const assert = require("node:assert/strict");
const rules = require("./checkout-rules");

const shirt = { id: "p1", name: "Oxford Shirt", status: "published", price: 7500, sizeStock: { M: 4 } };

test("rejects a price sent by the browser", () => {
    assert.throws(() => rules.rejectClientPrices({ productId: "p1", price: 1 }));
});

test("prices from the product document", () => {
    const priced = rules.price([{ productId: "p1", size: "M", color: "navy", quantity: 2 }], { p1: shirt });
    assert.equal(priced[0].priceCents, 7500);
    assert.equal(rules.sum(priced), 15000);
});

test("same cart shares an idempotency key", () => {
    const lines = [
        { productId: "p1", size: "M", color: "navy", quantity: 1 },
        { productId: "p2", size: "S", color: "", quantity: 1 },
    ];
    assert.equal(rules.idempotencyKey("uid-1", lines), rules.idempotencyKey("uid-1", [lines[1], lines[0]]));
});

test("paid is written only from awaiting_payment", () => {
    assert.equal(rules.canPay(rules.PaymentStates.AwaitingPayment), true);
    assert.equal(rules.canPay(rules.PaymentStates.Paid), false);
    assert.equal(rules.canRefund(rules.PaymentStates.Paid), true);
});

test("ambassador commission is ten percent", () => {
    assert.equal(rules.commissionAmount("ambassador", 15000), 1500);
    assert.equal(rules.commissionAmount("customer", 15000), null);
});

test("live stripe keys are refused", () => {
    assert.throws(() => rules.requireTestKey("sk_live_example"));
    rules.requireTestKey("sk_test_example");
});

const rollback = require("./rollback-log");

test("rollback export log keeps integer cents", () => {
    const row = rollback.entry("abNWxsrk8dLviFIKp1Qr", {
        email: "info@kobneti.com",
        paymentStatus: "refunded",
        total: 2200,
        items: [{ productId: "p1", quantity: 1, priceCents: 2200 }],
    }, "refunded");
    assert.equal(row.source, "firebase");
    assert.equal(row.total, 2200);
    assert.equal(row.windowId, rollback.WINDOW_ID);
    assert.equal(row.orderId, "abNWxsrk8dLviFIKp1Qr");
});
