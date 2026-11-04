import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
    plugins: [react()],
    build: {
        outDir: '../wwwroot',
        emptyOutDir: true,
    },
    server: {
        // Fast port: SpaProxy i .NET forventer Vite akkurat her (SpaProxyServerUrl i .csproj).
        port: 5173,
        strictPort: true,
        proxy: {
            '/api': 'http://localhost:5127',
        },
    },
})
