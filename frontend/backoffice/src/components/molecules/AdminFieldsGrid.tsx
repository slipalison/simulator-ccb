// ---------------------------------------------------------------------------
// AdminFieldsGrid — read-only key/value renderer for entity detail pages (T-5)
// ---------------------------------------------------------------------------

interface Field {
  label: string;
  value: string | number | null | undefined;
  testId?: string;
}

interface AdminFieldsGridProps {
  fields: Field[];
}

export function AdminFieldsGrid({ fields }: AdminFieldsGridProps) {
  return (
    <dl
      className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-x-6 gap-y-4"
      data-testid="fields-grid"
    >
      {fields.map((field) => (
        <div key={field.label} className="flex flex-col gap-0.5">
          <dt className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
            {field.label}
          </dt>
          <dd
            className="text-sm font-medium"
            data-testid={field.testId}
          >
            {field.value != null && field.value !== "" ? String(field.value) : "—"}
          </dd>
        </div>
      ))}
    </dl>
  );
}
