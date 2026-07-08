const API = "http://localhost:5288/api/auth";

class ForgotPassword {
    constructor() {
        this.form = document.getElementById("forgotForm");
        this.loginId = document.getElementById("loginId");
        this.btn = document.querySelector(".login-btn");

        this.init();
    }

    init() {
        this.form.addEventListener("submit", (e) => this.handleSubmit(e));
    }

    showError(msg) {
        const el = document.getElementById("loginIdError");
        el.textContent = msg;
        el.classList.add("show");
    }

    clearError() {
        const el = document.getElementById("loginIdError");
        el.textContent = "";
        el.classList.remove("show");
    }

    async handleSubmit(e) {
        e.preventDefault();

        const loginId = this.loginId.value.trim();

        if (!loginId) {
            this.showError("Login ID is required");
            return;
        }

        this.clearError();
        this.setLoading(true);

        try {
            const res = await fetch(API + "/forgot-password", {
                method: "POST",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({ loginId })
            });

            const msg = await res.text();

            if (!res.ok) throw new Error(msg);

            document.getElementById("forgotForm").style.display = "none";
            document.getElementById("successMessage").classList.add("show");

            // chuyển sang reset
            setTimeout(() => {
                window.location.href = "reset.html";
            }, 1500);

        } catch (err) {
            this.showError(err.message);
        } finally {
            this.setLoading(false);
        }
    }

    setLoading(loading) {
        this.btn.classList.toggle("loading", loading);
        this.btn.disabled = loading;
    }
}

document.addEventListener("DOMContentLoaded", () => new ForgotPassword());