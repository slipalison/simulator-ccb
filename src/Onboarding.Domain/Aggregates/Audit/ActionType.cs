namespace Onboarding.Domain.Aggregates.Audit;

/// <summary>
/// Represents the type of admin action recorded in the audit log.
/// </summary>
public enum ActionType
{
    AdminCreated = 1,
    AdminBlocked = 2,
    AdminUnblocked = 3,
    AdminDeleted = 4,
    AdminLogin = 5,
    AdminLogout = 6,
    AdminPasswordChanged = 7,
    AdminProfileUpdated = 8,
    UserCreated = 9,
    UserBlocked = 10,
    UserUnblocked = 11,
    UserDeleted = 12,
    UserUpdated = 13,
    // Phase 35 — Admin Management
    AdminEdited = 14,
    AdminPasswordReset = 15,
    AdminDisabled = 16,
    AdminReactivated = 17,
    // Phase 37 — Company/Employee actions
    CompanyRegistered = 18,
    EmployeeCreated = 19,
    EmployeeEdited = 20,
    EmployeeBlocked = 21,
    EmployeeUnblocked = 22,
    EmployeePasswordReset = 23,
    EmployeeDeleted = 24,
    AccessGroupChanged = 25,
    // Phase 38 — Company update
    CompanyUpdated = 26,
    // Phase 44 — Custom Access Groups CRUD
    AccessGroupCreated = 27,
    AccessGroupUpdated = 28,
    AccessGroupDeleted = 29,
    // Phase 45 — Fund management actions
    FundoCreated = 30,
    FundoUpdated = 31,
    FundoStatusChanged = 32,
    FundoDeleted = 33,
    ConsultoriaCreated = 34,
    ConsultoriaUpdated = 35,
    CustodianteCreated = 36,
    CustodianteUpdated = 37,
    CedenteCreated = 38,
    CedenteUpdated = 39,
    TipoAtivoCreated = 40,
    TipoAtivoUpdated = 41,
    FundoCedenteAssociated = 42,
    FundoCedenteUpdated = 43,
    FundoCedenteRemoved = 44,
    // Phase 50 — relationship aggregate actions (D-22)
    RelFundoCedenteCreated = 45,
    RelFundoCedenteLimiteUpdated = 46,
    RelFundoCedenteStatusChanged = 47,
    RelCedenteTipoAtivoCreated = 48,
    RelCedenteTipoAtivoLimiteUpdated = 49,
    RelCedenteTipoAtivoStatusChanged = 50,
    RelFundoTipoAtivoCreated = 51,
    RelFundoTipoAtivoLimiteUpdated = 52,
    RelFundoTipoAtivoStatusChanged = 53,
}