// js/Auth/login.js
const API = "http://localhost:5288/api/auth";

class LoginForm {
    constructor() {
        this.form = document.getElementById('loginForm');
        this.username = document.getElementById('username');
        this.password = document.getElementById('password');
        this.btn = document.querySelector('.login-btn');

        this.init();
    }

    init() {
        this.form.addEventListener('submit', (e) => this.handleLogin(e));

        // toggle password
        document.getElementById('passwordToggle')
            .addEventListener('click', () => {
                this.password.type =
                    this.password.type === 'password' ? 'text' : 'password';
            });
    }

    showError(id, msg) {
        const el = document.getElementById(id + "Error");
        el.textContent = msg;
        el.classList.add("show");
    }

    clearError(id) {
        const el = document.getElementById(id + "Error");
        el.textContent = "";
        el.classList.remove("show");
    }

    async handleLogin(e) {
        e.preventDefault();

        const username = this.username.value.trim();
        const password = this.password.value.trim();

        if (!username) {
            this.showError("username", "Username is required");
            return;
        } else this.clearError("username");

        if (!password) {
            this.showError("password", "Password is required");
            return;
        } else this.clearError("password");

        this.setLoading(true);

        try {
            const res = await fetch(API + "/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password })
            });

            const data = await res.json();

            if (!res.ok) {
                throw new Error(data);
            }

            // lưu token
            localStorage.setItem("token", data.token);
            localStorage.setItem("userId", data.userId);
            localStorage.setItem("role", data.roleId);   // FIX: lưu roleId để fallback hoạt động đúng

            this.showSuccess();

        } catch (err) {
            this.showError("Wrong password" || "Login failed");
        } finally {
            this.setLoading(false);
        }
    }

    setLoading(loading) {
        this.btn.classList.toggle("loading", loading);
        this.btn.disabled = loading;
    }

showSuccess() {
        // Ẩn form, header và link đăng ký để không gian thoáng hơn
        document.getElementById("loginForm").style.display = "none";
        document.querySelector(".login-header").style.display = "none";
        document.querySelector(".register-link").style.display = "none";
        
        // Hiển thị thông báo thành công
        document.getElementById("successMessage").classList.add("show");

        // Chuyển hướng sau 1.5 giây
        setTimeout(() => {
            window.location.href = "../Dashboard/dashboard.html";
        }, 1500);
    }
}
function goToRegister() {
    window.location.href = "register.html";
}
document.addEventListener("DOMContentLoaded", () => new LoginForm());