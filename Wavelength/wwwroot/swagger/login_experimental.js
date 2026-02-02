(function () {
    let loginWindow = null;
    let authState = false;
    let loginButton = null;
    let loginToolsButton = null;

    function addLoginButton() {
        const container = document.querySelector(".auth-wrapper");
        if (!container) return;

        if (container.querySelector(".swagger-login-btn")) return;

        const btn = document.createElement("button");
        btn.innerText = authState ? "Logout" : "Login";
        btn.className = "authorize btn swagger-login-btn";
        //btn.style.marginRight = ".5rem";
        btn.onclick = openLoginWindow;
        loginButton = btn;

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
        loginToolsButton = btn;

        container.appendChild(btn);

        console.log("Login button added to Swagger UI Tools");
    }

    function openLoginWindow() {
        if (authState) {
            logout();
            console.log("Logged out from Swagger");
            return;
        }

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

            const payload = event.data.payload;
            if (payload) {
                login(payload);
                console.log("JWT token successfully applied to Swagger:", payload.jwtToken);
            }
            
            if (loginWindow && !loginWindow.closed) {
                loginWindow.close();
                console.log("Login window closed");
            }
            loginWindow = null;
        }
    });

    async function refreshLogin() {
        const refreshToken = localStorage.getItem("refreshToken");
        if (!refreshToken) {
            console.warn("No refresh token found");
            return null;
        }
        console.warn("Refresh token:", refreshToken);

        const expiry = Number(localStorage.getItem("jwtTokenExpiry"));
        const now = Math.floor(Date.now() / 1000);

        if (now < expiry) {
            const jwtToken = localStorage.getItem("jwtToken");
            window.ui.preauthorizeApiKey("Bearer", jwtToken);
            authState = true;
            if (loginButton) {
                loginButton.innerText = "Logout";
            }
            if (loginToolsButton) {
                loginToolsButton.innerText = "Logout";
            }
            return;
        }

        try {
            const response = await fetch("/auth/refresh", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    token: refreshToken
                })
            });

            if (!response.ok) {
                console.warn("Refresh failed:", response.status);
                logout();
                return;
            }

            const payload = await response.json();
            console.log(payload);
            login(payload);
            return;

        } catch (err) {
            console.error("Refresh error:", err);
            logout();
            return;
        }
    }

    function login(payload) {
        authState = true;
        localStorage.setItem("jwtToken", payload.jwtToken);
        localStorage.setItem("refreshToken", payload.refreshToken);

        const expiresInSeconds = payload.expires;
        // UTC now i sekunder
        const nowUtcSeconds = Math.floor(Date.now() / 1000);
        // Beregn udløbstidspunkt
        const expiryTimestamp = nowUtcSeconds + expiresInSeconds;
        // Gem i localStorage
        localStorage.setItem("jwtTokenExpiry", expiryTimestamp);

        window.ui.preauthorizeApiKey("Bearer", payload.jwtToken);

        if (loginButton) {
            loginButton.innerText = "Logout";
        }
        if (loginToolsButton) {
            loginToolsButton.innerText = "Logout";
        }
    }

    function logout() {
        authState = false;
        window.ui.authActions.logout(["Bearer"]);
        localStorage.removeItem("jwtToken");
        localStorage.removeItem("jwtTokenExpiry");
        localStorage.removeItem("refreshToken");

        if (loginButton) {
            loginButton.innerText = "Login";
        }
        if (loginToolsButton) {
            loginToolsButton.innerText = "Logout";
        }
    }

    // When Swagger UI is ready
    window.addEventListener("load", async () => {
        const observer = new MutationObserver(async () => {
            if (!window.ui) return;

            observer.disconnect();

            await refreshLogin();

            window.ui.getConfigs().requestInterceptor = async function (req) {
                console.log("Execute pressed:", req);

                await refreshLogin();

                return req;
            };
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    });

    // When DOM is ready, fetch swagger.json and wait for .auth-wrapper
    document.addEventListener("DOMContentLoaded", async () => {
        const authWrapperObserver = new MutationObserver(() => {
            const container = document.querySelector(".auth-wrapper");
            if (container) {
                const ogAuthBtn = document.querySelector(".btn.authorize");
                if (ogAuthBtn) {
                    ogAuthBtn.style.display = "none"
                }
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
