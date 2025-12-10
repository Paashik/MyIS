import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "/api": {
        // Backend Kestrel слушает порт 5000 (см. лог "Now listening on: http://0.0.0.0:5000")
        // поэтому дев-прокси Vite направляем именно туда
        target: "http://localhost:5000",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});