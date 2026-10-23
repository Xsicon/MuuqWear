const ACTION_URL = "https://muuqwear.com/auth/action";

function brandedActionLink(firebaseLink) {
    let parsed;
    try {
        parsed = new URL(String(firebaseLink || ""));
    } catch {
        return String(firebaseLink || "");
    }

    const params = new URLSearchParams();
    const take = (url) => {
        for (const key of ["mode", "oobCode", "apiKey", "lang"]) {
            const value = url.searchParams.get(key);
            if (value) params.set(key, value);
        }
    };
    take(parsed);
    const nested = parsed.searchParams.get("link");
    if (nested) {
        try { take(new URL(nested)); } catch { /* ignore malformed wrapper */ }
    }

    if (!params.get("oobCode")) return parsed.toString();
    if (!params.get("mode")) params.set("mode", "verifyEmail");
    return `${ACTION_URL}?${params.toString()}`;
}

module.exports = { ACTION_URL, brandedActionLink };
