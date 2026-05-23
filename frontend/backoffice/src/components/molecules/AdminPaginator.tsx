// ---------------------------------------------------------------------------
// AdminPaginator — numeric paginator for admin Fundos lists (D-27, D-31)
// Reusable across all 7 admin Fundos list pages.
// ---------------------------------------------------------------------------

import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";

interface AdminPaginatorProps {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}

export function AdminPaginator({
  page,
  pageSize,
  totalCount,
  onPageChange,
}: AdminPaginatorProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  if (totalPages <= 1 && totalCount <= pageSize) {
    return null;
  }

  const isFirst = page <= 1;
  const isLast = page >= totalPages;

  // Build visible page numbers (up to 7, with ellipsis)
  const getPageNumbers = (): (number | "ellipsis")[] => {
    if (totalPages <= 7) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }
    const pages: (number | "ellipsis")[] = [1];
    if (page > 3) pages.push("ellipsis");
    for (let p = Math.max(2, page - 1); p <= Math.min(totalPages - 1, page + 1); p++) {
      pages.push(p);
    }
    if (page < totalPages - 2) pages.push("ellipsis");
    pages.push(totalPages);
    return pages;
  };

  const pageNumbers = getPageNumbers();

  return (
    <nav aria-label={`${L.paginatorPage} ${page} ${L.paginatorOf} ${totalPages}`} data-testid="admin-paginator">
      <ul className="flex items-center gap-1 justify-center">
        <li>
          <button
            type="button"
            onClick={() => onPageChange(page - 1)}
            disabled={isFirst}
            aria-label={L.paginatorPrevious}
            data-testid="paginator-prev"
            className="px-3 py-1.5 text-sm rounded-md border border-input bg-background hover:bg-accent disabled:opacity-50 disabled:pointer-events-none transition-colors"
          >
            {L.paginatorPrevious}
          </button>
        </li>
        {pageNumbers.map((p, idx) =>
          p === "ellipsis" ? (
            <li key={`ellipsis-${idx}`} aria-hidden="true">
              <span className="px-2 text-sm text-muted-foreground">…</span>
            </li>
          ) : (
            <li key={p}>
              <button
                type="button"
                onClick={() => onPageChange(p)}
                aria-label={`${L.paginatorPage} ${p}`}
                aria-current={p === page ? "page" : undefined}
                data-testid={`paginator-page-${p}`}
                className={
                  p === page
                    ? "px-3 py-1.5 text-sm rounded-md border border-primary bg-primary text-primary-foreground font-medium transition-colors"
                    : "px-3 py-1.5 text-sm rounded-md border border-input bg-background hover:bg-accent transition-colors"
                }
              >
                {p}
              </button>
            </li>
          )
        )}
        <li>
          <button
            type="button"
            onClick={() => onPageChange(page + 1)}
            disabled={isLast}
            aria-label={L.paginatorNext}
            data-testid="paginator-next"
            className="px-3 py-1.5 text-sm rounded-md border border-input bg-background hover:bg-accent disabled:opacity-50 disabled:pointer-events-none transition-colors"
          >
            {L.paginatorNext}
          </button>
        </li>
      </ul>
      <p className="text-xs text-muted-foreground text-center mt-2" data-testid="paginator-info">
        {L.paginatorPage} {page} {L.paginatorOf} {totalPages}
      </p>
    </nav>
  );
}
