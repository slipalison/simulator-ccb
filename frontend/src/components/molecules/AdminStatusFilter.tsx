import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface AdminStatusFilterProps {
  value: string; // "all" | "active" | "blocked" | "deleted"
  onChange: (value: string) => void;
  disabled?: boolean;
}

const STATUS_OPTIONS = [
  { value: "all", label: "Todos" },
  { value: "active", label: "Ativo" },
  { value: "blocked", label: "Bloqueado" },
  { value: "deleted", label: "Deletado" },
];

export function AdminStatusFilter({
  value,
  onChange,
  disabled = false,
}: AdminStatusFilterProps) {
  return (
    <Select value={value} onValueChange={onChange} disabled={disabled}>
      <SelectTrigger className="w-[180px]" data-testid="status-filter">
        <SelectValue placeholder="Status" />
      </SelectTrigger>
      <SelectContent>
        {STATUS_OPTIONS.map((opt) => (
          <SelectItem key={opt.value} value={opt.value} data-testid={`status-${opt.value}`}>
            {opt.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
