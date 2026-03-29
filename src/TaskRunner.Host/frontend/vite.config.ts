import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    }
  },
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true
  },
  server: {
    proxy: {
      '/api': 'http://localhost:5050',
      '/hubs': {
        target: 'http://localhost:5050',
        ws: true
      }
    }
  }
})
