import { _electron as electron, expect, test } from "@playwright/test";
import electronPath from "electron";
import { applicationEntry } from "./fixtures/test-data.js";

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
        expect(await application.evaluate(({ app }) => app.getName())).toBe(
            "Inkwell",
        );
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
        ).toBeDisabled();
        await expect(page.getByText(/网络连接已断开/)).toBeVisible();
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
