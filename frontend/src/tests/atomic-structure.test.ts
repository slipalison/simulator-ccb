import { describe, it, expect } from "vitest";
import { existsSync } from "fs";
import path from "path";

const srcComponents = path.resolve(__dirname, "../components");

describe("FRONT-01: Atomic Design structure", () => {
  it("has an atom component (AppButton)", () => {
    expect(existsSync(path.join(srcComponents, "atoms/AppButton.tsx"))).toBe(true);
  });

  it("has a molecule component (LabeledField)", () => {
    expect(existsSync(path.join(srcComponents, "molecules/LabeledField.tsx"))).toBe(true);
  });

  it("has an organism component (ExampleForm)", () => {
    expect(existsSync(path.join(srcComponents, "organisms/ExampleForm.tsx"))).toBe(true);
  });

  it("has a template component (PageLayout)", () => {
    expect(existsSync(path.join(srcComponents, "templates/PageLayout.tsx"))).toBe(true);
  });

  it("has a page component (HomePage or NotFoundPage)", () => {
    const hasHome = existsSync(path.join(srcComponents, "pages/HomePage.tsx"));
    const hasNotFound = existsSync(path.join(srcComponents, "pages/NotFoundPage.tsx"));
    expect(hasHome || hasNotFound).toBe(true);
  });
});
