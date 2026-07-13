import { defineConfig, externalizeDepsPlugin } from 'electron-vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath } from 'node:url'

const projectRoot = fileURLToPath(new URL('.', import.meta.url))

export default defineConfig({
  main: {
    plugins: [externalizeDepsPlugin()],
    build: {
      rollupOptions: {
        input: {
          index: fileURLToPath(new URL('electron/main.ts', import.meta.url)),
        },
      },
    },
  },
  preload: {
    plugins: [externalizeDepsPlugin()],
    build: {
      rollupOptions: {
        input: {
          index: fileURLToPath(new URL('electron/preload.ts', import.meta.url)),
        },
        output: {
          format: 'cjs',
        },
      },
    },
  },
  renderer: {
    root: projectRoot,
    plugins: [react()],
    build: {
      rollupOptions: {
        input: {
          index: fileURLToPath(new URL('index.html', import.meta.url)),
        },
      },
    },
  },
})