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
            '/api': {
                target: 'http://localhost:5127',
                // Behold «localhost:5173» som Host, og send den opprinnelige adressen i X-Forwarded-*.
                // Da sender .NET brukeren tilbake hit etter innlogging, ikke til 5127.
                changeOrigin: false,
                xfwd: true,
            },
        },
    },
})
