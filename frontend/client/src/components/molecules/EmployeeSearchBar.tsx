import { useState, useEffect } from "react";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface EmployeeSearchBarProps {
  onSearchChange: (search: string) => void;
  onStatusChange: (status: string) => void;
  searchValue: string;
  statusValue: string;
  disabled?: boolean;
}

const STATUS_OPTIONS = [
  { value: "all", label: "Todos" },
  { value: "active", label: "Ativos" },
  { value: "blocked", label: "Bloqueados" },
] as const;

export { STATUS_OPTIONS };

export function EmployeeSearchBar({
  onSearchChange,
  onStatusChange,
  searchValue,
  statusValue,
  disabled = false,
}: EmployeeSearchBarProps) {
  const [inputValue, setInputValue] = useState(searchValue);

  // Debounced search — 300ms delay
  useEffect(() => {
    const timer = setTimeout(() => {
      onSearchChange(inputValue);
    }, 300);
    return () => clearTimeout(timer);
  }, [inputValue, onSearchChange]);

  return (
    <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
      <Input
        placeholder="Buscar por nome ou email..."
        value={inputValue}
        onChange={(e) => setInputValue(e.target.value)}
        disabled={disabled}
        className="sm:max-w-[300px]"
        data-testid="employee-search-input"
      />
      <Select
        value={statusValue}
        onValueChange={onStatusChange}
        disabled={disabled}
      >
        <SelectTrigger
          className="sm:max-w-[180px]"
          data-testid="employee-status-filter"
        >
          <SelectValue placeholder="Filtrar status" />
        </SelectTrigger>
        <SelectContent>
          {STATUS_OPTIONS.map((option) => (
            <SelectItem key={option.value} value={option.value}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}