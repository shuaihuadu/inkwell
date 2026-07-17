import { _electron as electron, expect, test } from "@playwright/test";
import electronPath from "electron";
import { createServer } from "node:http";

const applicationEntry = "out/main/index.js";

test("renders the prototype-aligned login experience", async ({
    browserName,
}, testInfo) => {
    const application = await electron.launch({
        executablePath: electronPath as unknown as string,
        args: [
            applicationEntry,
            `--user-data-dir=${testInfo.outputPath(`${browserName}-user-data`)}`,
        ],
        env: {
            ...process.env,
            INKWELL_WEBAPI_URL: "http://127.0.0.1:1",
        },
    });

    try {
        const page = await application.firstWindow();
        await expect(
            page.getByRole("heading", { name: "Inkwell", exact: true }),
        ).toBeVisible();
        await expect(
            page.getByRole("heading", { name: "Inkwell Agent 平台" }),
        ).toBeVisible();
        const logo = page.locator(".login-heading img");
        await expect(logo).toBeVisible();
        expect(
            await logo.evaluate(
                (element) => (element as HTMLImageElement).naturalWidth,
            ),
        ).toBeGreaterThan(0);
        await expect(page.getByPlaceholder("请输入账号")).toHaveValue("");
        await expect(page.getByPlaceholder("请输入密码")).toHaveValue("");
        await expect(
            page.locator(".login-form .ant-form-item-label"),
        ).toHaveCount(0);
        await expect(page.locator('link[rel="icon"]')).toHaveAttribute(
            "href",
            "./logo.svg",
        );
        await expect(
            page.getByRole("button", { name: /登\s*录/ }),
        ).toBeEnabled();
        await expect(
            page.getByText("如忘记密码或需要开通账号，请联系系统管理员"),
        ).toBeVisible();

        const brandBox = await page.locator(".login-brand").boundingBox();
        const brandTitleBox = await page
            .getByRole("heading", { name: "Inkwell", exact: true })
            .boundingBox();
        const formBox = await page.locator(".login-form-wrap").boundingBox();
        expect(brandBox).not.toBeNull();
        expect(brandTitleBox).not.toBeNull();
        expect(formBox).not.toBeNull();
        expect(brandBox!.x + brandBox!.width).toBeLessThanOrEqual(formBox!.x);
        expect(
            Math.abs(
                brandTitleBox!.y +
                    brandTitleBox!.height / 2 -
                    (brandBox!.y + brandBox!.height / 2),
            ),
        ).toBeLessThanOrEqual(1);
        expect(formBox!.width).toBeLessThanOrEqual(360);

        await page.screenshot({
            path: testInfo.outputPath("login.png"),
            fullPage: true,
        });
    } finally {
        await application.close();
    }
});

test("shows authentication errors and enters the workspace after login", async ({
    browserName,
}, testInfo) => {
    let loginAttempts = 0;
    const server = createServer((request, response) => {
        if (request.url === "/api/auth/login") {
            loginAttempts += 1;
            response.setHeader("Content-Type", "application/json");

            if (loginAttempts === 1) {
                response.statusCode = 401;
                response.end(
                    JSON.stringify({ detail: "Invalid username or password." }),
                );
                return;
            }

            response.end(
                JSON.stringify({
                    userId: "0198a96d-19e4-7000-8000-000000000001",
                    username: "admin",
                    isSuper: true,
                    sessionToken: "test-session-token",
                    expiresAt: "2026-07-15T00:00:00Z",
                }),
            );
            return;
        }

        if (
            request.url === "/api/agents/mine" ||
            request.url === "/api/models"
        ) {
            response.setHeader("Content-Type", "application/json");
            response.end("[]");
            return;
        }

        if (request.url === "/api/auth/unlock") {
            response.statusCode = 204;
            response.end();
            return;
        }

        response.statusCode = 404;
        response.end();
    });
    await new Promise<void>((resolve) =>
        server.listen(0, "127.0.0.1", resolve),
    );
    const address = server.address();
    if (!address || typeof address === "string")
        throw new Error("Test server did not bind a TCP port.");

    const application = await electron.launch({
        executablePath: electronPath as unknown as string,
        args: [
            applicationEntry,
            `--user-data-dir=${testInfo.outputPath(`${browserName}-user-data`)}`,
        ],
        env: {
            ...process.env,
            INKWELL_WEBAPI_URL: `http://127.0.0.1:${address.port}`,
        },
    });

    try {
        const page = await application.firstWindow();
        const username = page.getByPlaceholder("请输入账号");
        const password = page.getByPlaceholder("请输入密码");
        const submit = page.getByRole("button", { name: /登\s*录/ });

        await username.fill("admin");
        await password.fill("wrong-password");
        await submit.click();
        await expect(page.getByText("账号或密码错误，请重试")).toBeVisible();
        await expect(username).toHaveValue("admin");
        await expect(password).toHaveValue("");
        await expect(password).toBeFocused();

        await password.fill("correct-password");
        await submit.click();
        await expect(
            page.getByRole("heading", { name: "Agent 库" }),
        ).toBeVisible();
        await expect(page.getByText("工作区", { exact: true })).toBeVisible();
        await expect(page.getByText("资源中心", { exact: true })).toBeVisible();
        await expect(page.getByText("系统管理", { exact: true })).toBeVisible();
        await expect(page.getByRole("button", { name: "Admin" })).toBeVisible();

        await page.getByRole("button", { name: "关于 Inkwell" }).click();
        await expect(
            page.getByRole("heading", { name: "Inkwell", exact: true }),
        ).toBeVisible();
        await expect(page.getByText("版本", { exact: true })).toBeVisible();
        await expect(page.getByTestId("app-version")).not.toHaveText("-");
        const qrCode = page.getByRole("img", { name: "公众号二维码" });
        await expect(qrCode).toBeVisible();
        expect(
            await qrCode.evaluate(
                (element) => (element as HTMLImageElement).naturalWidth,
            ),
        ).toBeGreaterThan(0);
        await page.keyboard.press("Escape");

        await page
            .getByRole("button", { name: "打开用户菜单" })
            .dispatchEvent("click");
        await expect(page.getByText("个人设置", { exact: true })).toBeVisible();
        await expect(page.getByText("管理", { exact: true })).toBeVisible();
        await page
            .getByText("个人设置", { exact: true })
            .dispatchEvent("click");
        await expect(
            page.getByRole("dialog", { name: "个人设置" }),
        ).toBeVisible();
        await expect(page.getByText("曜石紫", { exact: true })).toBeVisible();
        await expect(page.getByText("朱砂橙", { exact: true })).toBeVisible();
        await expect(page.getByText("碧海青", { exact: true })).toBeVisible();
        await page.getByText("亮色", { exact: true }).dispatchEvent("click");
        await page.getByText("朱砂橙", { exact: true }).dispatchEvent("click");
        await page.keyboard.press("Escape");
        await expect(page.locator("html")).toHaveAttribute(
            "data-theme",
            "terracotta",
        );
        await expect(page.locator("html")).toHaveAttribute(
            "data-appearance",
            "light",
        );

        const appearanceSwitch = page.getByRole("switch", { name: "切换外观" });
        await appearanceSwitch.dispatchEvent("click");
        await expect(page.locator("html")).toHaveAttribute(
            "data-appearance",
            "dark",
        );
        await expect(page.locator(".library-pane")).toHaveCSS(
            "background-color",
            "rgb(23, 20, 19)",
        );

        await page.getByRole("button", { name: /工具管理/ }).click();
        await expect(
            page.getByRole("heading", { name: "工具管理" }),
        ).toBeVisible();
        await expect(page.getByText("即将上线", { exact: true })).toBeVisible();

        await page.getByRole("button", { name: "Agent 空间" }).click();
        await expect(
            page.getByRole("heading", { name: "Agent 库" }),
        ).toBeVisible();
        await page.setViewportSize({ width: 1080, height: 720 });
        expect(
            await page.evaluate(() => document.documentElement.scrollWidth),
        ).toBeLessThanOrEqual(1080);
        await page.screenshot({
            path: testInfo.outputPath("workspace-dark-1080x720.png"),
            fullPage: true,
        });

        await application.evaluate(({ app }) => {
            app.emit("browser-window-blur", {} as never, null as never);
        });
        await expect(
            page.getByRole("heading", { name: "Agent 库" }),
        ).toBeVisible();
        await expect(
            page.getByRole("heading", { name: "Inkwell 已锁定" }),
        ).toHaveCount(0);

        await application.evaluate(({ powerMonitor }) => {
            powerMonitor.emit("lock-screen");
        });
        await expect(
            page.getByRole("heading", { name: "Inkwell 已锁定" }),
        ).toBeVisible();
        await expect(
            page.getByText("admin，请输入密码继续", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByRole("button", { name: "切换账号" }),
        ).toBeVisible();
        await expect(page.getByRole("button", { name: "登出" })).toBeVisible();
        await page.screenshot({
            path: testInfo.outputPath("lock-dark-1080x720.png"),
            fullPage: true,
        });
        await page.getByPlaceholder("密码").fill("correct-password");
        await page.keyboard.press("Enter");
        await expect(
            page.getByRole("heading", { name: "Agent 库" }),
        ).toBeVisible();
    } finally {
        await application.close();
        await new Promise<void>((resolve, reject) => {
            server.close((error) => (error ? reject(error) : resolve()));
        });
    }
});

test("hides system administration navigation from regular users", async ({
    browserName,
}, testInfo) => {
    const server = createServer((request, response) => {
        if (request.url === "/api/auth/login") {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    userId: "0198a96d-19e4-7000-8000-000000000002",
                    username: "member",
                    isSuper: false,
                    sessionToken: "member-session-token",
                    expiresAt: "2026-07-15T00:00:00Z",
                }),
            );
            return;
        }

        if (
            request.url === "/api/agents/mine" ||
            request.url === "/api/models"
        ) {
            response.setHeader("Content-Type", "application/json");
            response.end("[]");
            return;
        }

        response.statusCode = 404;
        response.end();
    });
    await new Promise<void>((resolve) =>
        server.listen(0, "127.0.0.1", resolve),
    );
    const address = server.address();
    if (!address || typeof address === "string")
        throw new Error("Test server did not bind a TCP port.");

    const application = await electron.launch({
        executablePath: electronPath as unknown as string,
        args: [
            applicationEntry,
            `--user-data-dir=${testInfo.outputPath(`${browserName}-user-data`)}`,
        ],
        env: {
            ...process.env,
            INKWELL_WEBAPI_URL: `http://127.0.0.1:${address.port}`,
        },
    });

    try {
        const page = await application.firstWindow();
        await page.getByPlaceholder("请输入账号").fill("member");
        await page.getByPlaceholder("请输入密码").fill("correct-password");
        await page.getByRole("button", { name: /登\s*录/ }).click();

        await expect(
            page.getByRole("heading", { name: "Agent 库" }),
        ).toBeVisible();
        await expect(page.getByText("系统管理", { exact: true })).toHaveCount(
            0,
        );
        await expect(page.getByRole("button", { name: "Admin" })).toHaveCount(
            0,
        );
        await expect(page.getByText("资源中心", { exact: true })).toBeVisible();
        await page
            .getByRole("button", { name: "打开用户菜单" })
            .dispatchEvent("click");
        await expect(page.getByText("个人设置", { exact: true })).toBeVisible();
        await expect(page.getByText("管理", { exact: true })).toHaveCount(0);
    } finally {
        await application.close();
        await new Promise<void>((resolve, reject) => {
            server.close((error) => (error ? reject(error) : resolve()));
        });
    }
});
