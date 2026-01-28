(function () {
    let loginWindow = null;
    let blazorUrl = null;

    async function loadBlazorUrl() {
        try {
            const response = await fetch("/swagger/v2/swagger.json");
            const spec = await response.json();

            // Check both root and info for x-blazor-url
            blazorUrl = spec["x-blazor-url"] || (spec.info && spec.info["x-blazor-url"]);
            if (!blazorUrl) {
                console.error("x-blazor-url not found in swagger.json");
            } else {
                console.log("x-blazor-url loaded:", blazorUrl);
            }
        } catch (err) {
            console.error("Could not fetch swagger.json:", err);
        }
    }

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

        console.log("Login button added to Swagger UI");
    }

    function openLoginWindow() {
        //if (!blazorUrl) {
        //    alert("x-blazor-url is missing in swagger.json");
        //    return;
        //}

        // Redirect to Blazor Login with ReturnUrl = static page in API

        //const redirectUrl = `${window.location.origin}/swagger/login-complete.html`;
        const loginUrl = `/swagger/auth.html`;

        const w = 500, h = 600;
        const left = window.screenX + (window.outerWidth - w) / 2;
        const top = window.screenY + (window.outerHeight - h) / 2;

        loginWindow = window.open(loginUrl, "BlazorLogin", `width=${w},height=${h},left=${left},top=${top}`);
        console.log("Login window opened:", loginUrl);
    }

    // Listen for message from the static page
    window.addEventListener("message", async (event) => {
        if (event.data && event.data.type === "login-complete") {
            console.log("Login complete message received from redirect page");
            try {
                const response = await fetch(`${blazorUrl}/auth/swaggertoken`, {
                    credentials: "include"
                });
                if (response.ok) {
                    const token = await response.text();
                    if (token) {
                        window.ui.preauthorizeApiKey("Bearer", token);
                        console.log("JWT token successfully applied to Swagger:", token);
                    } else {
                        console.warn("No token returned from /auth/swaggertoken");
                    }
                } else {
                    console.error("Failed to fetch token, status:", response.status);
                }
            } catch (err) {
                console.error("Could not fetch token:", err);
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
        await loadBlazorUrl();

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