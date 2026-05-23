namespace Onboarding.Application.Fundos.Queries.Admin;

/// <summary>
/// Cross-company admin query for a single Cedente by Id (D-8).
/// IgnoreQueryFilters applied — no tenant isolation. Admin use only.
/// Returns null if not found.
/// </summary>
public sealed record GetAdminCedenteByIdQuery(Guid Id);
