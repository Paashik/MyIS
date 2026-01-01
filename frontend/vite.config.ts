import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "/api": {
        // Backend Kestrel (dotnet watch) сейчас слушает http://localhost:5000 (см. лог "Now listening on: ...")
        // поэтому дев-прокси Vite направляем туда же.
        target: "http://localhost:5000",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
