// stripe-interop.js — bridge between Blazor and Stripe.js
// Mirrors the window-attached function pattern used elsewhere in this project.

window.mwStripe = {
    instance: null,
    elements: null,
    paymentElement: null,

    // 1. Initialize Stripe with publishable key (called once per checkout load)
    init: function (publishableKey) {
        if (!window.Stripe) {
            console.error("[Stripe] Stripe.js not loaded");
            return false;
        }
        this.instance = window.Stripe(publishableKey);
        return true;
    },

    // 2. Mount Stripe's payment element into the given selector
    mount: function (selector, clientSecret) {
        if (!this.instance) {
            console.error("[Stripe] mount() called before init()");
            return false;
        }

        this.elements = this.instance.elements({
            clientSecret: clientSecret,
            appearance: {
                theme: "stripe",
                variables: {
                    colorPrimary: "#1E2A47",
                    colorText: "#1E2A47",
                    colorDanger: "#c00",
                    fontFamily: "system-ui, sans-serif",
                    spacingUnit: "4px",
                    borderRadius: "4px"
                }
            }
        });

        this.paymentElement = this.elements.create("payment");
        this.paymentElement.mount(selector);
        return true;
    },

    // 3. Confirm the payment when user clicks Pay
    confirm: async function (returnUrl) {
        if (!this.instance || !this.elements) {
            return { success: false, error: "Stripe not initialized" };
        }

        const result = await this.instance.confirmPayment({
            elements: this.elements,
            confirmParams: { return_url: returnUrl },
            redirect: "if_required"   // stay on page unless 3DS demands a redirect
        });

        if (result.error) {
            return { success: false, error: result.error.message };
        }

        return {
            success: true,
            paymentIntentId: result.paymentIntent.id,
            status: result.paymentIntent.status
        };
    },

    // 4. Cleanup when leaving the page
    teardown: function () {
        if (this.paymentElement) {
            this.paymentElement.unmount();
            this.paymentElement = null;
        }
        this.elements = null;
        this.instance = null;
    }
};