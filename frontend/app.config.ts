import { createApp } from "vinxi";

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
        server: {
          host: "0.0.0.0",
          port: 5173,
          hmr: {
            host: "localhost",
            port: 5173,
            clientPort: 5173,
          },
          watch: {
            usePolling: true,   // Required: inotify events unreliable in Docker on Windows
            interval: 1000,
          },
        },
      },
    },
  ],
});
