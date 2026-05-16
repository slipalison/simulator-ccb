/**
 * Vitest tests for scripts/check-dev-env.mjs
 *
 * Uses injectable execSyncImpl to avoid real docker invocations.
 * Environment: node (not jsdom — this is a pure Node guard script).
 */

import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { run } from "./check-dev-env.mjs";

// Helper: stub execSync to return a controlled running-services string
function makeExecSync(output) {
  return vi.fn().mockReturnValue(output);
}

// Helper: stub execSync to throw (docker unavailable)
function makeExecSyncThrow(err = new Error("docker not found")) {
  return vi.fn().mockImplementation(() => {
    throw err;
  });
}

describe("check-dev-env guard", () => {
  let origAllowHostDev;

  beforeEach(() => {
    origAllowHostDev = process.env.ALLOW_HOST_DEV;
    delete process.env.ALLOW_HOST_DEV;
  });

  afterEach(() => {
    if (origAllowHostDev === undefined) {
      delete process.env.ALLOW_HOST_DEV;
    } else {
      process.env.ALLOW_HOST_DEV = origAllowHostDev;
    }
  });

  it("exits 0 when docker compose ps returns no matching service", () => {
    const execSync = makeExecSync("api\nkeycloak\npostgres\n");
    const result = run(["frontend-client"], execSync);
    expect(result).toBe(0);
    expect(execSync).toHaveBeenCalledOnce();
  });

  it("exits 1 when the target service IS running", () => {
    const execSync = makeExecSync("api\nfrontend-client\nkeycloak\n");
    const result = run(["frontend-client"], execSync);
    expect(result).toBe(1);
  });

  it("exits 1 when ANY of multiple target services is running", () => {
    const execSync = makeExecSync("api\nfrontend-backoffice\n");
    const result = run(["frontend-client", "frontend-backoffice"], execSync);
    expect(result).toBe(1);
  });

  it("exits 0 when none of multiple target services is running", () => {
    const execSync = makeExecSync("api\nkeycloak\n");
    const result = run(["frontend-client", "frontend-backoffice"], execSync);
    expect(result).toBe(0);
  });

  it("exits 0 when ALLOW_HOST_DEV=1 even if the service is running", () => {
    process.env.ALLOW_HOST_DEV = "1";
    const execSync = makeExecSync("api\nfrontend-client\n");
    const result = run(["frontend-client"], execSync);
    expect(result).toBe(0);
    // execSync must NOT be called — bypass is checked before docker query
    expect(execSync).not.toHaveBeenCalled();
  });

  it("exits 0 when ALLOW_HOST_DEV=true", () => {
    process.env.ALLOW_HOST_DEV = "true";
    const execSync = makeExecSync("frontend-client\n");
    const result = run(["frontend-client"], execSync);
    expect(result).toBe(0);
  });

  it("exits 0 when ALLOW_HOST_DEV=yes", () => {
    process.env.ALLOW_HOST_DEV = "yes";
    const execSync = makeExecSync("frontend-client\n");
    const result = run(["frontend-client"], execSync);
    expect(result).toBe(0);
  });

  it("exits 0 when docker compose is unavailable (execSync throws)", () => {
    const execSync = makeExecSyncThrow();
    const result = run(["frontend-client"], execSync);
    expect(result).toBe(0);
  });

  it("exits 0 when docker compose ps returns empty output", () => {
    const execSync = makeExecSync("");
    const result = run(["frontend-client"], execSync);
    expect(result).toBe(0);
  });
});
