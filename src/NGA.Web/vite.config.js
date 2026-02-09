import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

export default defineConfig({
    plugins: [vue()],
    resolve: {
        alias: {
            '@': path.resolve(__dirname, './src')
        }
    },
    build: {
        // 启用文件名 hash，确保文件更新后浏览器获取新版本
        rollupOptions: {
            output: {
                // JS 文件名带 hash
                entryFileNames: 'assets/[name].[hash].js',
                chunkFileNames: 'assets/[name].[hash].js',
                // CSS 文件名带 hash
                assetFileNames: 'assets/[name].[hash].[ext]'
            }
        },
        // 生成 manifest.json 用于版本跟踪
        manifest: true
    },
    server: {
        port: 5173,
        proxy: {
            '/api': {
                target: 'http://127.0.0.1:5000',
                changeOrigin: true
            }
        }
    }
})
