window.muuqFirebase = (function () {
    var version = "11.10.0";
    var auth = null;
    var authMod = null;
    var firestoreMod = null;
    var db = null;
    var initPromise = null;
    var totpSecret = null;
    var mfaResolver = null;
    var functionsBase = "";
    var imageHost = "https://firebasestorage.googleapis.com/v0/b/muuqwear-6e2e0.firebasestorage.app/o/";

    function load(name) {
        return import("https://www.gstatic.com/firebasejs/" + version + "/firebase-" + name + ".js");
    }

    function userSnapshot(user) {
        if (!user) return null;
        return {
            uid: user.uid,
            email: user.email || "",
            emailVerified: !!user.emailVerified
        };
    }

    function genericSignIn(error, kind) {
        var code = error && error.code ? error.code : "";
        if (code === "auth/invalid-credential" || code === "auth/user-not-found" ||
            code === "auth/wrong-password" || code === "auth/invalid-email") {
            return "Email or password is incorrect.";
        }
        if (code === "auth/email-already-in-use")
            return "An account with that email already exists.";
        if (code === "auth/weak-password")
            return "Choose a stronger password.";
        if (code === "auth/too-many-requests")
            return "Too many attempts. Wait a moment and try again.";
        if (code === "auth/invalid-verification-code")
            return "That authenticator code is not valid.";
        if (code === "auth/requires-recent-login")
            return "Sign in again, then turn the authenticator app on.";
        if (code === "auth/operation-not-allowed")
            return kind === "signup"
                ? "Email sign-up is not available yet."
                : "Authenticator-app sign-in is not available yet.";
        if (code === "auth/network-request-failed")
            return "Unable to reach sign-in. Check your connection and try again.";
        return "Unable to complete that request. Please try again.";
    }

    function totpId() {
        return authMod && authMod.TotpMultiFactorGenerator
            ? authMod.TotpMultiFactorGenerator.FACTOR_ID
            : "totp";
    }

    function enrolledTotp(user) {
        var factors = authMod.multiFactor(user).enrolledFactors || [];
        return factors.find(function (factor) { return factor.factorId === totpId(); }) || null;
    }

    function useEmulators(config) {
        return !!(config && (config.useEmulators === true || config.useEmulators === "true"));
    }

    async function sendBrandedVerification(user) {
        if (!user) return { ok: false, message: "Create the account again to resend the confirmation email." };
        var base = functionsBase || "";
        if (!base) return { ok: false, message: "Unable to send the confirmation email." };
        var token = await user.getIdToken();
        var response = await fetch(base.replace(/\/?$/, "/") + "sendVerificationEmail", {
            method: "POST",
            headers: {
                Authorization: "Bearer " + token,
                "Content-Type": "application/json"
            }
        });
        var payload = {};
        try { payload = await response.json(); } catch (error) { payload = {}; }
        if (!response.ok)
            return { ok: false, message: payload.error || "Unable to send the confirmation email." };
        return { ok: true };
    }

    async function doInit(config) {
        functionsBase = config && config.functionsBaseUrl ? String(config.functionsBaseUrl) : "";
        var emulators = useEmulators(config);
        var appMod = await load("app");
        var app = appMod.initializeApp({
            apiKey: config.apiKey,
            authDomain: config.authDomain,
            projectId: config.projectId,
            appId: config.appId,
            storageBucket: config.storageBucket,
            messagingSenderId: config.messagingSenderId
        });

        if (!emulators && config.appCheckSiteKey) {
            try {
                var appCheckMod = await load("app-check");
                appCheckMod.initializeAppCheck(app, {
                    provider: new appCheckMod.ReCaptchaEnterpriseProvider(config.appCheckSiteKey),
                    isTokenAutoRefreshEnabled: true
                });
            } catch (error) {
                console.warn("App Check did not start", error);
            }
        }

        try {
            firestoreMod = await load("firestore");
            if (emulators) {
                db = firestoreMod.getFirestore(app);
                firestoreMod.connectFirestoreEmulator(db, "127.0.0.1", 8080);
            } else {
                db = firestoreMod.initializeFirestore(app, {
                    localCache: firestoreMod.persistentLocalCache({
                        tabManager: firestoreMod.persistentMultipleTabManager()
                    })
                });
            }
        } catch (error) {
            console.warn("Firestore offline cache did not start", error);
            db = null;
            firestoreMod = null;
        }

        authMod = await load("auth");
        auth = authMod.getAuth(app);
        if (emulators) {
            authMod.connectAuthEmulator(auth, "http://127.0.0.1:9099", { disableWarnings: true });
        }
        await authMod.setPersistence(auth, authMod.browserLocalPersistence);
        await new Promise(function (resolve) {
            var unsubscribe = authMod.onAuthStateChanged(auth, function () {
                unsubscribe();
                resolve();
            });
        });
        return true;
    }

    function init(config) {
        if (!initPromise) {
            initPromise = doInit(config).catch(function (error) {
                initPromise = null;
                auth = null;
                console.warn("Firebase Auth did not start", error);
                return false;
            });
        }
        return initPromise;
    }

    async function withUser(action) {
        if (initPromise) await initPromise;
        if (!auth || !authMod) return { ok: false, message: "Sign-in needs a connection." };
        try {
            return await action();
        } catch (error) {
            return { ok: false, message: genericSignIn(error, "signin") };
        }
    }

    return {
        init: init,
        currentUser: async function () {
            if (initPromise) await initPromise;
            return userSnapshot(auth && auth.currentUser);
        },
        signIn: function (email, password) {
            return withUser(async function () {
                try {
                    var credential = await authMod.signInWithEmailAndPassword(auth, email, password);
                    return {
                        ok: true,
                        uid: credential.user.uid,
                        email: credential.user.email || "",
                        emailVerified: !!credential.user.emailVerified
                    };
                } catch (error) {
                    if (error && error.code === "auth/multi-factor-auth-required") {
                        mfaResolver = authMod.getMultiFactorResolver(auth, error);
                        return {
                            ok: false,
                            mfaRequired: true,
                            message: "Enter the code from your authenticator app."
                        };
                    }
                    throw error;
                }
            });
        },
        completeTotpSignIn: function (code) {
            return withUser(async function () {
                if (!mfaResolver)
                    return { ok: false, message: "Sign in again." };
                var hint = (mfaResolver.hints || []).find(function (item) {
                    return item.factorId === totpId();
                });
                if (!hint)
                    return { ok: false, message: "Authenticator sign-in is not set up for this account." };
                var assertion = authMod.TotpMultiFactorGenerator.assertionForSignIn(
                    hint.uid,
                    (code || "").trim());
                var credential = await mfaResolver.resolveSignIn(assertion);
                mfaResolver = null;
                return { ok: true, uid: credential.user.uid, email: credential.user.email || "" };
            });
        },
        mfaStatus: async function () {
            if (initPromise) await initPromise;
            if (!auth || !auth.currentUser || !authMod)
                return { ok: true, enrolled: false, signedIn: false };
            return {
                ok: true,
                enrolled: !!enrolledTotp(auth.currentUser),
                signedIn: true
            };
        },
        startTotpEnroll: function () {
            return withUser(async function () {
                var user = auth.currentUser;
                if (!user)
                    return { ok: false, message: "Sign in to turn on an authenticator app." };
                var session = await authMod.multiFactor(user).getSession();
                totpSecret = await authMod.TotpMultiFactorGenerator.generateSecret(session);
                return {
                    ok: true,
                    secretKey: totpSecret.secretKey,
                    qrCodeUrl: totpSecret.generateQrCodeUrl(user.email || "customer", "MuuqWear")
                };
            });
        },
        finishTotpEnroll: function (code) {
            return withUser(async function () {
                if (!totpSecret)
                    return { ok: false, message: "Start authenticator setup again." };
                var assertion = authMod.TotpMultiFactorGenerator.assertionForEnrollment(
                    totpSecret,
                    (code || "").trim());
                await authMod.multiFactor(auth.currentUser).enroll(assertion, "Authenticator app");
                totpSecret = null;
                return { ok: true, enrolled: true };
            });
        },
        unenrollTotp: function () {
            return withUser(async function () {
                var user = auth.currentUser;
                if (!user)
                    return { ok: false, message: "Sign in to change authenticator settings." };
                var factor = enrolledTotp(user);
                if (!factor)
                    return { ok: true, enrolled: false };
                await authMod.multiFactor(user).unenroll(factor);
                return { ok: true, enrolled: false };
            });
        },
        signUp: function (email, password, displayName) {
            return withUser(async function () {
                try {
                    var credential = await authMod.createUserWithEmailAndPassword(auth, email, password);
                    if (displayName)
                        await authMod.updateProfile(credential.user, { displayName: displayName });
                    var mailed = await sendBrandedVerification(credential.user);
                    if (!mailed.ok) return mailed;
                    return {
                        ok: true,
                        uid: credential.user.uid,
                        email: credential.user.email || "",
                        emailVerified: !!credential.user.emailVerified
                    };
                } catch (error) {
                    return { ok: false, message: genericSignIn(error, "signup") };
                }
            });
        },
        sendEmailVerification: function () {
            return withUser(async function () {
                if (!auth.currentUser)
                    return { ok: false, message: "Create the account again to resend the confirmation email." };
                return await sendBrandedVerification(auth.currentUser);
            });
        },
        signOut: function () {
            return withUser(async function () {
                await authMod.signOut(auth);
                return { ok: true };
            });
        },
        sendPasswordReset: function (email) {
            return withUser(async function () {
                await authMod.sendPasswordResetEmail(auth, email, {
                    url: "https://muuqwear.com/auth/action",
                    handleCodeInApp: true
                });
                return { ok: true };
            });
        },
        getIdToken: async function (forceRefresh) {
            if (initPromise) await initPromise;
            if (!auth || !auth.currentUser) return null;
            return await auth.currentUser.getIdToken(!!forceRefresh);
        },
        listProducts: function () { return catalogQuery("products"); },
        listCollections: function () { return catalogQuery("collections"); },
        listJournal: function () { return catalogQuery("content"); },
        getProduct: async function (id) {
            if (initPromise) await initPromise;
            if (!db || !firestoreMod || !id) return null;
            try {
                var snap = await firestoreMod.getDoc(firestoreMod.doc(db, "products", String(id)));
                if (!snap.exists()) return null;
                var row = toCatalogRow(snap);
                return row.status === "published" ? row : null;
            } catch (error) {
                console.warn("catalog get failed", error);
                return null;
            }
        }
    };

    async function catalogQuery(name) {
        if (initPromise) await initPromise;
        if (!db || !firestoreMod) return [];
        try {
            var q = firestoreMod.query(
                firestoreMod.collection(db, name),
                firestoreMod.where("status", "==", "published"));
            var snap = await firestoreMod.getDocs(q);
            return snap.docs.map(toCatalogRow);
        } catch (error) {
            console.warn("catalog list failed", name, error);
            return [];
        }
    }

    function toCatalogRow(snap) {
        var data = snap.data() || {};
        return {
            id: snap.id,
            name: str(data.name) || str(data.title) || snap.id,
            title: str(data.title),
            body: str(data.body),
            price: num(data.price),
            status: str(data.status) || "draft",
            sizeStock: toMap(data.sizeStock),
            imagePaths: strs(data.imagePaths).map(imageUrl),
            imageUrl: imageUrl(data.imageUrl),
            colors: strs(data.colors),
            collectionTags: strs(data.collectionTags),
            collectionId: str(data.collectionId),
            description: str(data.description),
            sku: str(data.sku),
            category: str(data.category),
            author: str(data.author),
            excerpt: str(data.excerpt),
            slug: str(data.slug),
            createdAt: ts(data.createdAt)
        };
    }

    function imageUrl(path) {
        var value = str(path);
        if (!value) return "";
        if (/^https?:/i.test(value)) return value;
        return imageHost + encodeURIComponent(value.replace(/^\//, "")) + "?alt=media";
    }

    function str(value) {
        return value == null ? "" : String(value);
    }

    function num(value) {
        var n = Number(value);
        return isFinite(n) ? n : 0;
    }

    function strs(value) {
        return Array.isArray(value) ? value.map(str).filter(Boolean) : [];
    }

    function toMap(value) {
        var out = {};
        if (!value || typeof value !== "object") return out;
        Object.keys(value).forEach(function (key) { out[key] = num(value[key]); });
        return out;
    }

    function ts(value) {
        if (!value) return null;
        if (typeof value.toDate === "function") return value.toDate().toISOString();
        if (typeof value === "string") return value;
        return null;
    }
})();
