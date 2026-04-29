import { useState, useEffect, useCallback } from "react";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { AdminSearchBar } from "@/components/molecules/AdminSearchBar";
import { AdminStatusFilter } from "@/components/molecules/AdminStatusFilter";
import { AdminPagination } from "@/components/molecules/AdminPagination";
import { listCompanies } from "@/lib/admin-api";
import { toast } from "sonner";
import type { CompanySummaryDto, PaginatedResult } from "@/lib/admin-api";
import { Building2 } from "lucide-react";

export function AdminCompaniesPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [status, setStatus] = useState("all");
  const [result, setResult] = useState<PaginatedResult<CompanySummaryDto> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isError, setIsError] = useState(false);

  const fetchCompanies = useCallback(async () => {
    setIsLoading(true);
    setIsError(false);
    try {
      const data = await listCompanies({
        page,
        pageSize: 20,
        search: debouncedSearch || undefined,
        status: status === "all" ? undefined : status,
      });
      setResult(data);
    } catch {
      setIsError(true);
      toast.error("Falha ao carregar empresas", { description: "Tente novamente." });
    } finally {
      setIsLoading(false);
    }
  }, [page, debouncedSearch, status]);

  useEffect(() => { fetchCompanies(); }, [fetchCompanies]);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);

  useEffect(() => { setPage(1); }, [debouncedSearch, status]);

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-col gap-4">
          <div className="flex items-center gap-2">
            <Building2 className="h-5 w-5 text-primary" />
            <h2 className="text-xl font-semibold">Empresas Cadastradas</h2>
          </div>
          <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
            <AdminSearchBar
              value={search}
              onChange={setSearch}
              placeholder="Buscar por razão social, CNPJ ou email"
              disabled={isLoading}
            />
            <AdminStatusFilter
              value={status}
              onChange={setStatus}
              disabled={isLoading}
            />
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {isLoading && !result ? (
          <div className="space-y-3">
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="h-12 bg-muted animate-pulse rounded" />
            ))}
          </div>
        ) : isError ? (
          <div className="text-center py-8 text-muted-foreground">
            <p>Erro ao carregar empresas.</p>
            <button onClick={fetchCompanies} className="text-primary underline mt-2">Tentar novamente</button>
          </div>
        ) : !result || result.totalCount === 0 ? (
          <div className="text-center py-8 text-muted-foreground" data-testid="empty-state">
            Nenhuma empresa encontrada.
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full" data-testid="companies-table">
                <thead>
                  <tr className="border-b text-sm text-muted-foreground">
                    <th className="text-left py-3 px-2">Razão Social</th>
                    <th className="text-left py-3 px-2">CNPJ</th>
                    <th className="text-left py-3 px-2">Email</th>
                    <th className="text-left py-3 px-2">Telefone</th>
                    <th className="text-left py-3 px-2">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {result.items.map((company) => (
                    <tr key={company.id} className="border-b hover:bg-muted/50 transition-colors" data-testid={`company-row-${company.id}`}>
                      <td className="py-3 px-2 font-medium">{company.razaoSocial}</td>
                      <td className="py-3 px-2 font-mono text-sm">{company.cnpj || "—"}</td>
                      <td className="py-3 px-2 text-sm">{company.email}</td>
                      <td className="py-3 px-2 text-sm">{company.phone}</td>
                      <td className="py-3 px-2">
                        <span className={`inline-flex items-center rounded-full px-2 py-1 text-xs font-medium ${company.isDeleted ? "bg-red-100 text-red-800" : "bg-green-100 text-green-800"}`}>
                          {company.isDeleted ? "Excluída" : "Ativa"}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {result.totalCount > 0 && (
              <div className="mt-4">
                <AdminPagination
                  page={result.page}
                  pageSize={result.pageSize}
                  totalCount={result.totalCount}
                  onPageChange={setPage}
                />
              </div>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}