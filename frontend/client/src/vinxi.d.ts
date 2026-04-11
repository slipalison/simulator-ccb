declare module "vinxi" {
  import type { App } from "nitropack";
  export function createApp(config: Record<string, unknown>): App;
}
