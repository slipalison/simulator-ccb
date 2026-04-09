import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
  PaginationEllipsis,
} from "@/components/ui/pagination";

interface AdminPaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}

export function AdminPagination({
  page,
  pageSize,
  totalCount,
  onPageChange,
}: AdminPaginationProps) {
  const totalPages = Math.ceil(totalCount / pageSize);

  if (totalPages <= 0) {
    return null;
  }

  // Build page numbers with ellipsis logic
  const getPageNumbers = (): (number | string)[] => {
    const pages: (number | string)[] = [];

    if (totalPages <= 7) {
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      pages.push(1, 2);
      if (page > 3) pages.push("ellipsis-start");
      if (page > 2 && page < totalPages - 1) pages.push(page);
      if (page < totalPages - 2) pages.push("ellipsis-end");
      pages.push(totalPages - 1, totalPages);
    }

    // Deduplicate and sort
    const seen = new Set<number>();
    const unique: (number | string)[] = [];
    for (const p of pages) {
      if (typeof p === "string") {
        // Avoid consecutive ellipsis
        const last = unique[unique.length - 1];
        if (last !== p) {
          unique.push(p);
        }
      } else if (!seen.has(p)) {
        seen.add(p);
        unique.push(p);
      }
    }

    return unique;
  };

  const pageNumbers = getPageNumbers();

  return (
    <Pagination data-testid="pagination">
      <PaginationContent>
        <PaginationItem>
          <PaginationPrevious
            href="#"
            onClick={(e) => {
              e.preventDefault();
              onPageChange(page - 1);
            }}
            aria-disabled={page === 1}
            className={page === 1 ? "pointer-events-none opacity-50" : ""}
            data-testid="prev-button"
          />
        </PaginationItem>

        {pageNumbers.map((p, idx) =>
          typeof p === "string" ? (
            <PaginationItem key={p} data-testid="ellipsis">
              <PaginationEllipsis />
            </PaginationItem>
          ) : (
            <PaginationItem key={p}>
              <PaginationLink
                href="#"
                onClick={(e) => {
                  e.preventDefault();
                  onPageChange(p);
                }}
                isActive={p === page}
                data-testid={`page-${p}`}
              >
                {p}
              </PaginationLink>
            </PaginationItem>
          )
        )}

        <PaginationItem>
          <PaginationNext
            href="#"
            onClick={(e) => {
              e.preventDefault();
              onPageChange(page + 1);
            }}
            aria-disabled={page === totalPages}
            className={page === totalPages ? "pointer-events-none opacity-50" : ""}
            data-testid="next-button"
          />
        </PaginationItem>
      </PaginationContent>
      <p className="text-sm text-muted-foreground text-center mt-2" data-testid="page-info">
        Pagina {page} de {totalPages}
      </p>
    </Pagination>
  );
}
