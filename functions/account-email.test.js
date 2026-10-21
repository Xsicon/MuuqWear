const { test } = require("node:test");
const assert = require("node:assert/strict");
const { brandedHtml, brandedText } = require("./account-email-copy");

test("branded verify email includes the logo and button", () => {
    const html = brandedHtml("ada@muuqwear.com", "https://muuqwear.com/__/auth/action?oobCode=test");
    assert.match(html, /apple-touch-icon\.png/);
    assert.match(html, />Verify email</);
    assert.match(html, /ada@muuqwear\.com/);
    assert.match(html, /https:\/\/muuqwear\.com\/__\/auth\/action\?oobCode=test/);
    assert.doesNotMatch(html, /%</);
});

test("branded verify text includes the link", () => {
    const text = brandedText("ada@muuqwear.com", "https://example.test/verify");
    assert.match(text, /Confirm your email/);
    assert.match(text, /https:\/\/example\.test\/verify/);
});
