// ---------------------------------------------------------------------------
// Backoffice admin Fundos module locale strings (pt-BR)
// All user-facing strings used in admin Fundos pages/components.
// ---------------------------------------------------------------------------

export const adminFundosLocale = {
  // Navigation / sidebar
  sidebarFundosGroup: "Fundos",
  sidebarFundos: "Fundos",
  sidebarCedentes: "Cedentes",
  sidebarConsultoriasFundo: "Consultorias",
  sidebarCustodiantes: "Custodiantes",
  sidebarFundoCedentes: "Fundo-Cedentes",
  sidebarFundoTiposAtivos: "Fundo-Tipos Ativos",
  sidebarCedenteTiposAtivos: "Cedente-Tipos Ativos",

  // Common table columns
  colEmpresa: "Empresa",
  colNome: "Nome",
  colStatus: "Status",
  colCnpj: "CNPJ",
  colDocumento: "Documento",
  colTipo: "Tipo",
  colCreatedAt: "Criado em",
  colActions: "Ações",
  colFundo: "Fundo",
  colCedente: "Cedente",
  colTipoAtivo: "Tipo Ativo",
  colDataInicio: "Início",
  colDataFim: "Fim",
  colLimitePercentual: "Limite %",
  colLimiteValor: "Limite R$",

  // Pages
  pageFundosTitle: "Fundos",
  pageCedentesTitle: "Cedentes",
  pageConsultoriasTitle: "Consultorias de Fundo",
  pageCustodiantesTitle: "Custodiantes",
  pageFundoCedentesTitle: "Fundo-Cedentes",
  pageFundoTiposAtivosTitle: "Fundo-Tipos Ativos",
  pageCedenteTiposAtivosTitle: "Cedente-Tipos Ativos",

  // Detail page
  detailFundoTitle: "Detalhe do Fundo",
  detailCedenteTitle: "Detalhe do Cedente",
  detailConsultoriaTitle: "Detalhe da Consultoria",
  detailCustodianteTitle: "Detalhe do Custodiante",
  detailSectionHistorico: "Histórico de Auditoria",
  detailFieldId: "ID",
  detailFieldEmpresa: "Empresa",
  detailFieldNome: "Nome",
  detailFieldRazaoSocial: "Razão Social",
  detailFieldNomeFantasia: "Nome Fantasia",
  detailFieldCnpj: "CNPJ",
  detailFieldDocumento: "Documento",
  detailFieldStatus: "Status",
  detailFieldTipo: "Tipo",
  detailFieldClasseAnbima: "Classe Anbima",
  detailFieldSegmento: "Segmento",
  detailFieldDataConstituicao: "Data Constituição",
  detailFieldEmail: "Email",
  detailFieldTelefone: "Telefone",
  detailFieldEndereco: "Endereço",
  detailFieldCedenteId: "ID Cedente",
  detailFieldCodigoInterno: "Código Interno",

  // Filter / search
  filterEmpresaPlaceholder: "Filtrar por empresa",
  filterEmpresaTodas: "Todas as empresas",
  searchPlaceholder: "Buscar...",

  // Paginator
  paginatorPrevious: "Anterior",
  paginatorNext: "Próximo",
  paginatorPage: "Página",
  paginatorOf: "de",

  // States
  loadingData: "Carregando...",
  emptyState: "Nenhum registro encontrado.",
  errorLoadingData: "Erro ao carregar dados.",
  notFound: "Registro não encontrado.",
  invalidId: "ID inválido.",

  // Status labels (enum values from backend)
  statusRascunho: "Rascunho",
  statusAtivo: "Ativo",
  statusSuspenso: "Suspenso",
  statusEmLiquidacao: "Em Liquidação",
  statusEncerrado: "Encerrado",
  statusInativo: "Inativo",
  statusHistorico: "Histórico",

  // Fundo tipo labels
  tipoFundoAberto: "Aberto",
  tipoFundoFechado: "Fechado",

  // Cedente tipo labels
  tipoCedentePF: "Pessoa Física",
  tipoCedentePJ: "Pessoa Jurídica",

  // Audit history
  auditNoHistory: "Sem histórico de auditoria.",
  auditLoadingHistory: "Carregando histórico...",
  auditErrorHistory: "Erro ao carregar histórico.",
  auditTimestamp: "Data/Hora",
  auditAdmin: "Administrador",
  auditAction: "Ação",
  auditDetails: "Detalhes",

  // Back button
  backToList: "Voltar para lista",
} as const;
