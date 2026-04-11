// ---------------------------------------------------------------------------
// Password strength calculation algorithm
// ---------------------------------------------------------------------------
// Score: 0-100 based on objective criteria
// - minLength (>= 8): +20
// - hasUpper: +15
// - hasLower: +15
// - hasDigit: +15
// - hasSpecial: +20
// - length >= 12: +15
//
// Levels:
//   0-19:   very-weak
//   20-39:  weak
//   40-59:  medium
//   60-79:  strong
//   80-100: very-strong
// ---------------------------------------------------------------------------

export function calculatePasswordStrength(password: string): {
  score: number;
  level: "very-weak" | "weak" | "medium" | "strong" | "very-strong";
  checks: {
    minLength: boolean;
    hasUpper: boolean;
    hasLower: boolean;
    hasDigit: boolean;
    hasSpecial: boolean;
    length12Plus: boolean;
  };
} {
  const checks = {
    minLength: password.length >= 8,
    hasUpper: /[A-Z]/.test(password),
    hasLower: /[a-z]/.test(password),
    hasDigit: /\d/.test(password),
    hasSpecial: /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(password),
    length12Plus: password.length >= 12,
  };

  let score = 0;
  if (checks.minLength) score += 20;
  if (checks.hasUpper) score += 15;
  if (checks.hasLower) score += 15;
  if (checks.hasDigit) score += 15;
  if (checks.hasSpecial) score += 20;
  if (checks.length12Plus) score += 15;

  let level: "very-weak" | "weak" | "medium" | "strong" | "very-strong";
  if (score < 20) level = "very-weak";
  else if (score < 40) level = "weak";
  else if (score < 60) level = "medium";
  else if (score < 80) level = "strong";
  else level = "very-strong";

  return { score, level, checks };
}
