import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig(({ mode }) => ({
  base: process.env.VITE_BASE_PATH ?? (mode === 'production' ? '/SmartBusinessManagementSystem/' : '/'),
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5017',
      '/health': 'http://localhost:5017',
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    passWithNoTests: true,
  },
}))
