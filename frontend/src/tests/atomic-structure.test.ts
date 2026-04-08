import { describe, it, expect } from "vitest";
import { existsSync } from "fs";
import path from "path";

const srcComponents = path.resolve(__dirname, "../components");

describe("FRONT-01: Atomic Design structure", () => {
  it("has an atom component (ThemeToggle)", () => {
    expect(existsSync(path.join(srcComponents, "atoms/ThemeToggle.tsx"))).toBe(true);
  });

  it("has a molecule component (PasswordField)", () => {
    expect(existsSync(path.join(srcComponents, "molecules/PasswordField.tsx"))).toBe(true);
  });

  it("has an organism component (Header)", () => {
    expect(existsSync(path.join(srcComponents, "organisms/Header.tsx"))).toBe(true);
  });

  it("has a page component (NotFoundPage)", () => {
    const hasNotFound = existsSync(path.join(srcComponents, "pages/NotFoundPage.tsx"));
    expect(hasNotFound).toBe(true);
  });
});
