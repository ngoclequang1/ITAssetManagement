const API_URL = "http://localhost:5288/api/auth/register";

document.getElementById("registerForm")
    .addEventListener("submit", async function (e) {
        e.preventDefault();

        const username = document.getElementById("username").value.trim();
        const email = document.getElementById("email").value.trim();
        const password = document.getElementById("password").value.trim();

        try {
            const res = await fetch(API_URL, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    username: username,
                    email: email,
                    password: password
                })
            });

            const data = await res.text();

            if (res.ok) {
                // Hiển thị Modal đẹp thay vì dùng alert()
                document.getElementById("successModal").classList.add("show");
                
                // Đợi 2 giây để người dùng xem thông báo, sau đó chuyển hướng
                setTimeout(() => {
                    window.location.href = "login.html";
                }, 2000);
            } else {
                alert("Error: " + data);
            }

        } catch (err) {
            console.error(err);
            alert("Cannot connect to server");
        }
    });