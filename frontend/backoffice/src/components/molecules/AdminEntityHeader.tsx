// ---------------------------------------------------------------------------
// AdminEntityHeader — shared header for entity detail pages (T-5)
// Displays entity name + empresa + status badge (read-only).
// ---------------------------------------------------------------------------

import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";

// Status color map — mirrors Phase 50 spec STATUS_COLORS
const STATUS_BADGE_CLASSES: Record<string, string> = {
  RASCUNHO: "bg-gray-500",
  ATIVO: "bg-green-500",
  SUSPENSO: "bg-yellow-500",
  EM_LIQUIDACAO: "bg-orange-500",
  ENCERRADO: "bg-red-500",
  INATIVO: "bg-gray-400",
  HISTORICO: "bg-blue-400",
};

function getStatusLabel(status: string): string {
  const map: Record<string, string> = {
    RASCUNHO: L.statusRascunho,
    ATIVO: L.statusAtivo,
    SUSPENSO: L.statusSuspenso,
    EM_LIQUIDACAO: L.statusEmLiquidacao,
    ENCERRADO: L.statusEncerrado,
    INATIVO: L.statusInativo,
    HISTORICO: L.statusHistorico,
  };
  return map[status] ?? status;
}

interface AdminEntityHeaderProps {
  title: string;
  nome: string;
  empresaNome: string;
  status: string;
  onBack?: () => void;
}

export function AdminEntityHeader({
  title,
  nome,
  empresaNome,
  status,
  onBack,
}: AdminEntityHeaderProps) {
  const badgeClass = STATUS_BADGE_CLASSES[status] ?? "bg-gray-500";

  return (
    <div className="mb-6" data-testid="entity-header">
      <div className="flex items-center gap-2 mb-1">
        {onBack && (
          <button
            type="button"
            onClick={onBack}
            className="text-sm text-muted-foreground hover:text-foreground transition-colors mr-1"
            data-testid="entity-back-button"
          >
            ← {L.backToList}
          </button>
        )}
      </div>
      <p className="text-xs text-muted-foreground uppercase tracking-wider">{title}</p>
      <div className="flex items-center gap-3 mt-1 flex-wrap">
        <h1
          className="text-2xl font-bold"
          data-testid="entity-header-nome"
        >
          {nome}
        </h1>
        <span
          className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium text-white ${badgeClass}`}
          data-testid="entity-header-status"
          aria-label={`Status: ${getStatusLabel(status)}`}
        >
          {getStatusLabel(status)}
        </span>
      </div>
      <p
        className="text-sm text-muted-foreground mt-0.5"
        data-testid="entity-header-empresa"
      >
        {L.detailFieldEmpresa}: {empresaNome}
      </p>
    </div>
  );
}
