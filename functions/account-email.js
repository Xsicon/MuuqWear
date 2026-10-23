const admin = require("firebase-admin");
const store = require("./store");
const { brandedHtml, brandedText } = require("./account-email-copy");
const { ACTION_URL, brandedActionLink } = require("./account-email-link");

const FROM = process.env.POSTMARK_FROM || "MuuqWear <noreply@muuqwear.com>";

function postmarkToken() {
    return process.env.POSTMARK_SERVER_TOKEN || process.env["postmark-server-token"] || "";
}

async function sendPostmark(to, subject, html, text) {
    const token = postmarkToken();
    if (!token) throw new Error("Mail is not configured.");
    const response = await fetch("https://api.postmarkapp.com/email", {
        method: "POST",
        headers: {
            Accept: "application/json",
            "Content-Type": "application/json",
            "X-Postmark-Server-Token": token
        },
        body: JSON.stringify({
            From: FROM,
            To: to,
            Subject: subject,
            HtmlBody: html,
            TextBody: text,
            MessageStream: "outbound",
            Tag: "verify-email"
        })
    });
    if (!response.ok) {
        const body = await response.text();
        throw new Error(body.slice(0, 300) || "The confirmation email could not be sent.");
    }
}

async function sendVerificationEmail(req, res) {
    store.allowOrigin(req, res);
    if (req.method === "OPTIONS") {
        res.status(204).send("");
        return;
    }
    if (req.method !== "POST") {
        store.json(res, 405, { error: "POST only" });
        return;
    }

    try {
        const user = await store.caller(req);
        if (!user.uid || !user.email) {
            store.json(res, 401, { error: "Sign in to send the confirmation email." });
            return;
        }
        const record = await admin.auth().getUser(user.uid);
        if (record.emailVerified) {
            store.json(res, 200, { ok: true, alreadyVerified: true });
            return;
        }
        const firebaseLink = await admin.auth().generateEmailVerificationLink(record.email, {
            url: ACTION_URL,
            handleCodeInApp: true
        });
        const link = brandedActionLink(firebaseLink);
        await sendPostmark(
            record.email,
            "Confirm your MuuqWear email",
            brandedHtml(record.email, link),
            brandedText(record.email, link)
        );
        store.json(res, 200, { ok: true });
    } catch (error) {
        console.error("sendVerificationEmail failed");
        store.json(res, 500, { error: "Unable to send the confirmation email. Please try again." });
    }
}

module.exports = { sendVerificationEmail };
