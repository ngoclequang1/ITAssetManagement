const API = "http://localhost:5288/api/auth";

class ResetPassword {
    constructor() {
        this.form = document.getElementById("resetForm");
        this.btn = document.querySelector(".login-btn");

        this.init();
    }

    init() {
        this.form.addEventListener("submit", (e) => this.handleSubmit(e));
    }

    async handleSubmit(e) {
        e.preventDefault();

        const loginId = document.getElementById("loginId").value.trim();
        const otp = document.getElementById("otp").value.trim();
        const newPassword = document.getElementById("newPassword").value.trim();

        if (!loginId || !otp || !newPassword) {
            alert("All fields are required");
            return;
        }

        this.setLoading(true);

        try {
            const res = await fetch(API + "/reset-password", {
                method: "POST",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({ loginId, otp, newPassword })
            });

            const msg = await res.text();

            if (!res.ok) throw new Error(msg);

            document.getElementById("resetForm").style.display = "none";
            document.getElementById("successMessage").classList.add("show");

            setTimeout(() => {
                window.location.href = "login.html";
            }, 1500);

        } catch (err) {
            alert(err.message);
        } finally {
            this.setLoading(false);
        }
    }

    setLoading(loading) {
        this.btn.classList.toggle("loading", loading);
        this.btn.disabled = loading;
    }
}

document.addEventListener("DOMContentLoaded", () => new ResetPassword());