import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface StatusOption {
  value: string;
  label: string;
}

interface AdminStatusFilterProps {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  options?: StatusOption[];
}

const DEFAULT_STATUS_OPTIONS: StatusOption[] = [
  { value: "all", label: "Todos" },
  { value: "active", label: "Ativo" },
  { value: "blocked", label: "Bloqueado" },
  { value: "deleted", label: "Deletado" },
];

export const ADMIN_STATUS_OPTIONS: StatusOption[] = [
  { value: "all", label: "Todos" },
  { value: "active", label: "Ativo" },
  { value: "inactive", label: "Inativo" },
];

export function AdminStatusFilter({
  value,
  onChange,
  disabled = false,
  options,
}: AdminStatusFilterProps) {
  const resolvedOptions = options ?? DEFAULT_STATUS_OPTIONS;

  return (
    <Select value={value} onValueChange={onChange} disabled={disabled}>
      <SelectTrigger className="w-[180px]" data-testid="status-filter" aria-label="Filtrar por status">
        <SelectValue placeholder="Status" />
      </SelectTrigger>
      <SelectContent>
        {resolvedOptions.map((opt) => (
          <SelectItem key={opt.value} value={opt.value} data-testid={`status-${opt.value}`}>
            {opt.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}