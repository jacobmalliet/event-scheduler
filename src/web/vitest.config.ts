import { fileURLToPath, URL } from 'node:url';

import vue from '@vitejs/plugin-vue';
import { playwright } from '@vitest/browser-playwright';
import { defineConfig } from 'vitest/config';

// Kept separate from vite.config.ts so tests don't load the dev-server proxy or Vue DevTools.
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  test: {
    include: ['tests/**/*.test.ts'],
    setupFiles: ['./vitest.setup.ts'],
    // Real Chromium via Playwright, so components are tested against a real DOM and real events.
    browser: {
      enabled: true,
      provider: playwright(),
      headless: true,
      instances: [{ browser: 'chromium' }],
    },
  },
});
