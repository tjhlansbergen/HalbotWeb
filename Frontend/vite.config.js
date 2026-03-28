import { defineConfig } from "vite";
import { svelte } from "@sveltejs/vite-plugin-svelte";

export default defineConfig({
  plugins: [svelte()],
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "https://localhost:7263",
        changeOrigin: true,
        secure: false
      }
    }
  },
  build: {
    outDir: "../wwwroot",
    emptyOutDir: true
  }
});
