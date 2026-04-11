import { describe, it, expect } from "vitest";
import { calculatePasswordStrength } from "@/lib/password-strength";

describe("calculatePasswordStrength", () => {
  it('returns "very-weak" for "abc"', () => {
    const result = calculatePasswordStrength("abc");
    // stub - should fail because function doesn't exist yet
    expect(result.level).toBe("very-weak");
    expect(result.score).toBeLessThan(20);
  });

  it('returns "weak" for "abcdefgh"', () => {
    const result = calculatePasswordStrength("abcdefgh");
    // stub - should fail
    expect(result.level).toBe("weak");
    expect(result.checks.minLength).toBe(true);
    expect(result.checks.hasLower).toBe(true);
  });

  it('returns "medium" for "Abcdefgh"', () => {
    const result = calculatePasswordStrength("Abcdefgh");
    // stub - should fail
    expect(result.level).toBe("medium");
    expect(result.checks.hasUpper).toBe(true);
    expect(result.checks.hasLower).toBe(true);
  });

  it('returns "strong" for "Abcdefg1"', () => {
    const result = calculatePasswordStrength("Abcdefg1");
    // stub - should fail
    expect(result.level).toBe("strong");
    expect(result.checks.hasDigit).toBe(true);
    expect(result.checks.hasUpper).toBe(true);
    expect(result.checks.hasLower).toBe(true);
  });

  it('returns "very-strong" for "Abcdefg1!xyz"', () => {
    const result = calculatePasswordStrength("Abcdefg1!xyz");
    // stub - should fail
    expect(result.level).toBe("very-strong");
    expect(result.score).toBeGreaterThanOrEqual(80);
    expect(result.checks.length12Plus).toBe(true);
    expect(result.checks.hasSpecial).toBe(true);
  });
});
