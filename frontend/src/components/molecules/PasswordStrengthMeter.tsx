import { calculatePasswordStrength } from "@/lib/password-strength";

interface PasswordStrengthMeterProps {
  password: string;
}

const levelLabels: Record<string, string> = {
  "very-weak": "Muito Fraca",
  weak: "Fraca",
  medium: "Media",
  strong: "Forte",
  "very-strong": "Muito Forte",
};

const levelColors: Record<string, string> = {
  "very-weak": "bg-red-500",
  weak: "bg-orange-500",
  medium: "bg-yellow-500",
  strong: "bg-lime-500",
  "very-strong": "bg-green-500",
};

const checkLabels: Record<keyof ReturnType<typeof calculatePasswordStrength>["checks"], string> = {
  minLength: "Minimo 8 caracteres",
  hasUpper: "Letra maiuscula",
  hasLower: "Letra minuscula",
  hasDigit: "Numero",
  hasSpecial: "Caractere especial",
  length12Plus: "12+ caracteres",
};

/**
 * PasswordStrengthMeter: visual indicator of password strength
 * Shows colored progress bar + checklist of criteria
 */
export function PasswordStrengthMeter({ password }: PasswordStrengthMeterProps) {
  if (!password) return null;

  const { score, level, checks } = calculatePasswordStrength(password);
  const percentage = score;

  return (
    <div className="mt-2 space-y-2">
      {/* Progress bar */}
      <div className="h-2 w-full rounded-full bg-gray-200">
        <div
          className={`h-2 rounded-full transition-all duration-300 ${levelColors[level]}`}
          style={{ width: `${percentage}%` }}
        />
      </div>

      {/* Level label */}
      <p className="text-sm text-muted-foreground">{levelLabels[level]}</p>

      {/* Checklist */}
      <ul className="grid grid-cols-2 gap-1 text-xs">
        {(Object.entries(checks) as [keyof typeof checks, boolean][]).map(
          ([key, passed]) => (
            <li key={key} className={passed ? "text-green-600" : "text-gray-400"}>
              {passed ? "\u2713" : "\u2717"} {checkLabels[key]}
            </li>
          )
        )}
      </ul>
    </div>
  );
}
