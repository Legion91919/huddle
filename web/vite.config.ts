/// <reference types="vitest" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The API runs on http://localhost:5106 (see .agentic/runbook.md); the dev server proxies to it.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5106',
      '/health': 'http://localhost:5106',
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
  },
});
