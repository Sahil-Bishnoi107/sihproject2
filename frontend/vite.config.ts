/// <reference types="vitest/config" />
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    // Same-origin in development too: the API is proxied, so no CORS and no absolute URLs in the app.
    proxy: {
      "/api": "http://localhost:8080",
      "/hangfire": "http://localhost:8080",
      "/swagger": "http://localhost:8080",
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
  },
});
