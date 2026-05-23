// ---------------------------------------------------------------------------
// AdminSearchInput — debounced search for admin Fundos lists (D-31)
// Debounce 300ms per D-27. Reusable across all list pages.
// ---------------------------------------------------------------------------

import { useState, useEffect, useRef } from "react";
import { Search, X } from "lucide-react";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";

interface AdminSearchInputProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  "data-testid"?: string;
}

const DEBOUNCE_MS = 300;

export function AdminSearchInput({
  value,
  onChange,
  placeholder = L.searchPlaceholder,
  "data-testid": testId = "admin-search-input",
}: AdminSearchInputProps) {
  // Local state for immediate UI feedback; debounce fires onChange
  const [localValue, setLocalValue] = useState(value);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Sync external value changes (e.g., URL param clear)
  useEffect(() => {
    setLocalValue(value);
  }, [value]);

  function handleChange(next: string) {
    setLocalValue(next);
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    timeoutRef.current = setTimeout(() => {
      onChange(next);
    }, DEBOUNCE_MS);
  }

  function handleClear() {
    setLocalValue("");
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    onChange("");
  }

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (timeoutRef.current) clearTimeout(timeoutRef.current);
    };
  }, []);

  const inputId = "admin-search-field";

  return (
    <div className="relative w-64">
      <label htmlFor={inputId} className="sr-only">
        {placeholder}
      </label>
      <Search
        className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none"
        aria-hidden="true"
      />
      <input
        id={inputId}
        type="search"
        value={localValue}
        onChange={(e) => handleChange(e.target.value)}
        placeholder={placeholder}
        data-testid={testId}
        className="h-9 w-full rounded-md border border-input bg-background pl-8 pr-8 text-sm shadow-sm focus:outline-none focus:ring-1 focus:ring-ring"
      />
      {localValue && (
        <button
          type="button"
          onClick={handleClear}
          aria-label="Limpar busca"
          data-testid="search-clear-button"
          className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
        >
          <X className="h-4 w-4" aria-hidden="true" />
        </button>
      )}
    </div>
  );
}
