import { createApp } from "vinxi";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import tsconfigPaths from "vite-tsconfig-paths";

export default createApp({
  routers: [
    {
      name: "public",
      type: "static",
      dir: "./public",
    },
    {
      name: "auth",
      type: "http",
      handler: "./auth-server.ts",
      target: "server",
      base: "/auth",
    },
    {
      name: "api-proxy",
      type: "http",
      handler: "./server.ts",
      target: "server",
      base: "/api",
    },
    {
      name: "client",
      type: "spa",
      handler: "./index.html",
      vite: {
        server: {
          host: "0.0.0.0",
          port: 5174,
          hmr: { host: "localhost", port: 5174, clientPort: 5174 },
          watch: { usePolling: true, interval: 1000 },
        },
      },
      plugins: () => [tsconfigPaths(), react(), tailwindcss()],
    },
  ],
});
