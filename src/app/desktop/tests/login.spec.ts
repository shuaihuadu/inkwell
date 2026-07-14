import { _electron as electron, expect, test } from "@playwright/test";
import electronPath from "electron";
import { createServer } from "node:http";

const applicationEntry = "out/main/index.js";

test("renders the prototype-aligned login experience", async ({ browserName }, testInfo) => {
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
        await expect(page.getByText("v0.0.0 · Build 20260714")).toBeVisible();

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
                brandTitleBox!.y + brandTitleBox!.height / 2 -
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

test("shows authentication errors and enters the workspace after login", async ({ browserName }, testInfo) => {
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
    } finally {
        await application.close();
        await new Promise<void>((resolve, reject) => {
            server.close((error) => (error ? reject(error) : resolve()));
        });
    }
});
