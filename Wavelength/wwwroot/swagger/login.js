(function () {
    let loginWindow = null;

    function addLoginButton() {
        const container = document.querySelector(".auth-wrapper");
        if (!container) return;

        if (container.querySelector(".swagger-login-btn")) return;

        const btn = document.createElement("button");
        btn.innerText = "Login";
        btn.className = "authorize btn swagger-login-btn";
        btn.style.marginRight = ".5rem";
        btn.onclick = openLoginWindow;

        const authorizeBtn = container.firstChild;

        if (authorizeBtn) {
            // Insert login button before Authorize
            container.insertBefore(btn, authorizeBtn);
        } else {
            // Fallback: just append it at the end
            container.appendChild(btn);
        }

        console.log("Login button added to Swagger UI");
    }

    function addToolsLoginButton() {
        const container = document.querySelector(".tools-card .card-body");

        const btn = document.createElement("button");
        btn.innerText = "Login";
        btn.className = "nav-link";
        btn.onclick = openLoginWindow;

        container.appendChild(btn);

        console.log("Login button added to Swagger UI Tools");
    }

    function openLoginWindow() {
        const loginUrl = `/swagger/login.html`;

        const w = 500, h = 600;
        const left = window.screenX + (window.outerWidth - w) / 2;
        const top = window.screenY + (window.outerHeight - h) / 2;

        loginWindow = window.open(loginUrl, "Login", `width=${w},height=${h},left=${left},top=${top}`);
        console.log("Login window opened:", loginUrl);
    }

    // Listen for message from the static page
    window.addEventListener("message", async (event) => {
        if (event.data && event.data.type === "login-complete") {
            console.log("Login OK:", event.data.payload);

            const token = event.data.payload.jwtToken;
            if (token) {
                window.ui.preauthorizeApiKey("Bearer", token);
                console.log("JWT token successfully applied to Swagger:", token);
            }
            
            if (loginWindow && !loginWindow.closed) {
                loginWindow.close();
                console.log("Login window closed");
            }
            loginWindow = null;
        }
    });

    // When DOM is ready, fetch swagger.json and wait for .auth-wrapper
    document.addEventListener("DOMContentLoaded", async () => {
        const authWrapperObserver = new MutationObserver(() => {
            const container = document.querySelector(".auth-wrapper");
            if (container) {
                addLoginButton();
                authWrapperObserver.disconnect();
            }
        });

        const swaggerBootstrapObserver = new MutationObserver(() => {
            const container = document.querySelector(".tools-card .card-body");
            if (container) {
                addToolsLoginButton();
                swaggerBootstrapObserver.disconnect();
            }
        });

        authWrapperObserver.observe(document.body, { childList: true, subtree: true });
        swaggerBootstrapObserver.observe(document.body, { childList: true, subtree: true });
    });
})();