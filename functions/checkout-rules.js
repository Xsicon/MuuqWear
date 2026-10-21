const crypto = require("crypto");

const PaymentStates = {
    AwaitingPayment: "awaiting_payment",
    Paid: "paid",
    PaymentFailed: "payment_failed",
    Refunded: "refunded",
    Expired: "expired",
};

function rejectClientPrices(line) {
    if (!line || typeof line !== "object") return;
    for (const key of Object.keys(line)) {
        if (/^(price|total|amount)$/i.test(key)) {
            throw new Error("The browser cannot set a price.");
        }
    }
}

function price(lines, products) {
    if (!lines.length) throw new Error("The cart is empty.");
    return lines.map((line) => {
        if (line.quantity < 1) throw new Error("Quantity must be at least 1.");
        const product = products[line.productId];
        if (!product) throw new Error(`Unknown product ${line.productId}.`);
        if (product.status !== "published") throw new Error(`${product.name} is not for sale.`);
        const stock = Number(product.sizeStock?.[line.size] ?? 0);
        if (stock < line.quantity) throw new Error(`${product.name} size ${line.size} is out of stock.`);
        const priceCents = Number(product.price);
        return {
            productId: product.id,
            name: product.name,
            size: line.size,
            color: line.color || "",
            quantity: line.quantity,
            priceCents,
            totalCents: priceCents * line.quantity,
        };
    });
}

function sum(lines) {
    return lines.reduce((total, line) => total + line.totalCents, 0);
}

function moveStock(sizeStock, size, quantity, direction) {
    if (quantity < 1) throw new Error("Quantity must be at least 1.");
    if (direction !== 1 && direction !== -1) throw new Error("direction");
    const next = { ...(sizeStock || {}) };
    const current = Number(next[size] ?? 0);
    const updated = current + direction * quantity;
    if (updated < 0) throw new Error(`Size ${size} is out of stock.`);
    next[size] = updated;
    return next;
}

function idempotencyKey(customerKey, lines) {
    const payload = [...lines]
        .sort((a, b) => `${a.productId}:${a.size}:${a.color}`.localeCompare(`${b.productId}:${b.size}:${b.color}`))
        .map((line) => `${line.productId}:${line.size}:${line.color}:${line.quantity}`)
        .join("|");
    const hash = crypto.createHash("sha256").update(`${customerKey}\n${payload}`).digest("hex");
    return `checkout_${hash.slice(0, 32)}`;
}

function canPay(current) { return current === PaymentStates.AwaitingPayment; }
function canFail(current) { return current === PaymentStates.AwaitingPayment; }
function canExpire(current) { return current === PaymentStates.AwaitingPayment; }
function canRefund(current) { return current === PaymentStates.Paid; }

function commissionAmount(referralType, orderTotalCents) {
    if (String(referralType || "").toLowerCase() !== "ambassador") return null;
    if (orderTotalCents < 1) return null;
    return Math.round(orderTotalCents * 0.1);
}

function requireTestKey(secretKey) {
    if (!secretKey || !String(secretKey).startsWith("sk_test_")) {
        throw new Error("Stripe test keys only.");
    }
}

module.exports = {
    PaymentStates,
    rejectClientPrices,
    price,
    sum,
    moveStock,
    idempotencyKey,
    canPay,
    canFail,
    canExpire,
    canRefund,
    commissionAmount,
    requireTestKey,
    senderDisplayName: "MuuqWear",
};
