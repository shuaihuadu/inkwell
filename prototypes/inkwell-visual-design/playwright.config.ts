import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
    testDir: "./tests",
    fullyParallel: false,
    forbidOnly: !!process.env["CI"],
    retries: 0,
    workers: 1,
    reporter: "list",
    use: {
        baseURL: "http://localhost:4174",
        trace: "off",
        screenshot: "only-on-failure",
    },
    projects: [
        {
            name: "desktop-hd",
            use: {
                ...devices["Desktop Chrome"],
                viewport: { width: 1440, height: 900 },
            },
        },
        {
            name: "desktop-md",
            use: {
                ...devices["Desktop Chrome"],
                viewport: { width: 1280, height: 800 },
            },
        },
        {
            name: "tablet",
            use: {
                ...devices["Desktop Chrome"],
                viewport: { width: 768, height: 720 },
            },
        },
        {
            name: "mobile",
            use: {
                ...devices["Desktop Chrome"],
                viewport: { width: 390, height: 844 },
            },
        },
    ],
    webServer: {
        command: "npm run preview",
        url: "http://localhost:4174",
        reuseExistingServer: true,
        timeout: 30_000,
    },
});
