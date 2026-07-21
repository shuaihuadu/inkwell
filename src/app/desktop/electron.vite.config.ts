import { defineConfig, externalizeDepsPlugin } from "electron-vite";
import react from "@vitejs/plugin-react";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const projectRoot = fileURLToPath(new URL(".", import.meta.url));
const desktopPort = Number.parseInt(
    process.env.INKWELL_DESKTOP_PORT ?? "6888",
    10,
);
const commit =
    process.env.INKWELL_COMMIT_SHA ??
    execFileSync("git", ["rev-parse", "HEAD"], {
        cwd: projectRoot,
        encoding: "utf8",
    }).trim();
const buildNumber = process.env.INKWELL_BUILD_NUMBER ?? "dev";

export default defineConfig({
    main: {
        plugins: [externalizeDepsPlugin()],
        define: {
            __INKWELL_BUILD_NUMBER__: JSON.stringify(buildNumber),
            __INKWELL_COMMIT_SHA__: JSON.stringify(commit),
        },
        build: {
            rollupOptions: {
                input: {
                    index: fileURLToPath(
                        new URL("electron/main.ts", import.meta.url),
                    ),
                },
            },
        },
    },
    preload: {
        plugins: [externalizeDepsPlugin()],
        build: {
            rollupOptions: {
                input: {
                    index: fileURLToPath(
                        new URL("electron/preload.ts", import.meta.url),
                    ),
                },
                output: {
                    format: "cjs",
                },
            },
        },
    },
    renderer: {
        root: projectRoot,
        plugins: [react()],
        server: {
            host: "localhost",
            port: desktopPort,
            strictPort: true,
        },
        build: {
            rollupOptions: {
                input: {
                    index: fileURLToPath(
                        new URL("index.html", import.meta.url),
                    ),
                },
            },
        },
    },
});
