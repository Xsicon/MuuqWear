const { test } = require("node:test");
const assert = require("node:assert/strict");
const { brandedHtml, brandedText } = require("./account-email-copy");
const { brandedActionLink } = require("./account-email-link");

test("branded verify email includes the logo and button", () => {
    const html = brandedHtml("ada@muuqwear.com", "https://muuqwear.com/auth/action?oobCode=test");
    assert.match(html, /apple-touch-icon\.png/);
    assert.match(html, />Verify email</);
    assert.match(html, /ada@muuqwear\.com/);
    assert.match(html, /https:\/\/muuqwear\.com\/auth\/action\?oobCode=test/);
    assert.doesNotMatch(html, /%</);
});

test("firebase action links are rewritten onto muuqwear.com", () => {
    const firebase = "https://muuqwear-6e2e0.firebaseapp.com/__/auth/action?mode=verifyEmail&oobCode=abc&apiKey=key&continueUrl=https%3A%2F%2Fmuuqwear.com%2Flogin&lang=en";
    const branded = brandedActionLink(firebase);
    assert.match(branded, /^https:\/\/muuqwear\.com\/auth\/action\?/);
    assert.match(branded, /mode=verifyEmail/);
    assert.match(branded, /oobCode=abc/);
    assert.doesNotMatch(branded, /firebaseapp\.com/);
});

test("branded verify text includes the link", () => {
    const text = brandedText("ada@muuqwear.com", "https://example.test/verify");
    assert.match(text, /Confirm your email/);
    assert.match(text, /https:\/\/example\.test\/verify/);
});
