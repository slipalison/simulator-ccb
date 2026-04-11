interface PersonTypeRadioProps {
  value: "PF" | "PJ";
  onChange: (value: "PF" | "PJ") => void;
}

/**
 * PersonTypeRadio: custom radio button group for PF/PJ selection
 * Styled with Tailwind — not native radio
 */
export function PersonTypeRadio({ value, onChange }: PersonTypeRadioProps) {
  const options: { id: "PF" | "PJ"; label: string; description: string }[] = [
    {
      id: "PF",
      label: "Pessoa Física",
      description: "Cadastro com CPF",
    },
    {
      id: "PJ",
      label: "Pessoa Jurídica",
      description: "Cadastro com CNPJ",
    },
  ];

  return (
    <div className="flex gap-4" role="radiogroup" aria-label="Tipo de pessoa">
      {options.map((option) => {
        const isSelected = value === option.id;
        return (
          <button
            key={option.id}
            type="button"
            role="radio"
            aria-checked={isSelected}
            onClick={() => onChange(option.id)}
            className={`flex-1 rounded-lg border-2 p-4 text-left transition-all ${
              isSelected
                ? "border-primary bg-primary/5 ring-1 ring-primary"
                : "border-input bg-background hover:border-muted-foreground"
            }`}
          >
            <div className="flex items-center gap-2">
              {/* Custom radio indicator */}
              <div
                className={`flex h-5 w-5 items-center justify-center rounded-full border-2 transition-colors ${
                  isSelected
                    ? "border-primary bg-primary"
                    : "border-muted-foreground"
                }`}
              >
                {isSelected && (
                  <div className="h-2 w-2 rounded-full bg-white" />
                )}
              </div>
              <div>
                <span className="block text-sm font-medium text-foreground">
                  {option.label}
                </span>
                <span className="block text-xs text-muted-foreground">
                  {option.description}
                </span>
              </div>
            </div>
          </button>
        );
      })}
    </div>
  );
}
