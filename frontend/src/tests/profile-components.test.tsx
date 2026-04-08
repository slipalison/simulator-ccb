// ---------------------------------------------------------------------------
// RED test stubs — Profile components and API client
// ---------------------------------------------------------------------------
// These tests are intentionally FAILING.
// They define the expected behavior before implementation (TDD RED phase).
// Implement in plan 10-02.
// ---------------------------------------------------------------------------

import { describe, it, expect } from "vitest";

describe("ProfileField", () => {
  it("renders label and value", () => {
    expect(true, "RED stub — ProfileField not yet implemented").toBe(false);
  });
});

describe("ProfileBadge", () => {
  it("shows Pessoa Física for PF type", () => {
    expect(true, "RED stub — ProfileBadge not yet implemented").toBe(false);
  });

  it("shows Pessoa Jurídica for PJ type", () => {
    expect(true, "RED stub — ProfileBadge not yet implemented").toBe(false);
  });
});

describe("ProfileCard", () => {
  it("renders PF profile fields", () => {
    expect(true, "RED stub — ProfileCard not yet implemented").toBe(false);
  });

  it("renders PJ profile fields", () => {
    expect(true, "RED stub — ProfileCard not yet implemented").toBe(false);
  });

  it("does not show CPF field for PJ profiles", () => {
    expect(true, "RED stub — ProfileCard not yet implemented").toBe(false);
  });

  it("does not show CNPJ field for PF profiles", () => {
    expect(true, "RED stub — ProfileCard not yet implemented").toBe(false);
  });
});

describe("getProfileClient", () => {
  it("fetches profile with Bearer token", () => {
    expect(true, "RED stub — getProfileClient not yet implemented").toBe(false);
  });

  it("throws ProfileError when no token available", () => {
    expect(true, "RED stub — getProfileClient not yet implemented").toBe(false);
  });

  it("throws ProfileError on 401 response", () => {
    expect(true, "RED stub — getProfileClient not yet implemented").toBe(false);
  });
});
