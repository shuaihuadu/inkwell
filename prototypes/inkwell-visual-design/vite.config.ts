import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
    plugins: [react()],
    server: {
        port: 6900,
        strictPort: true,
    },
    preview: {
        port: 6901,
        strictPort: true,
    },
    build: {
        sourcemap: false,
        outDir: "dist",
    },
});
