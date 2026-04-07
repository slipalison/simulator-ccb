import { createApp } from "vinxi";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { fileURLToPath } from "node:url";

export default createApp({
  routers: [
    {
      name: "public",
      type: "static",
      dir: "./public",
    },
    {
      name: "client",
      type: "spa",
      handler: "./index.html",
      vite: {
        resolve: {
          alias: {
            "@": fileURLToPath(new URL("./src", import.meta.url)),
          },
        },
        server: {
          host: "0.0.0.0",
          port: 5173,
          hmr: { host: "localhost", port: 5173, clientPort: 5173 },
          watch: { usePolling: true, interval: 1000 },
        },
      },
      plugins: () => [react(), tailwindcss()],
    },
  ],
});
