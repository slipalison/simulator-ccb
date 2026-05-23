using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query to retrieve paginated admin audit log entries with optional filters.
/// </summary>
public sealed record GetAuditLogQuery(
    int Page = 1,
    int PageSize = 20,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    ActionType? ActionType = null,
    string? AdminUserName = null,
    string? EntityType = null,
    Guid? EntityId = null);

/// <summary>
/// DTO representing a single audit log entry in API responses.
/// </summary>
public sealed record AdminAuditLogDto(
    Guid Id,
    DateTimeOffset Timestamp,
    Guid AdminUserId,
    string AdminUserName,
    string ActionType,
    Guid? TargetUserId,
    string? TargetUserName,
    string? Details,
    string? IpAddress,
    string? EntityType,
    Guid? EntityId);

public sealed class GetAuditLogQueryHandler : IQueryHandler<GetAuditLogQuery, PaginatedResult<AdminAuditLogDto>>
{
    private readonly IAdminAuditLogRepository _repository;

    public GetAuditLogQueryHandler(IAdminAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedResult<AdminAuditLogDto>> HandleAsync(GetAuditLogQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            query.Page,
            query.PageSize,
            query.StartDate,
            query.EndDate,
            query.ActionType,
            query.AdminUserName,
            query.EntityType,
            query.EntityId,
            ct);

        var dtos = items.Select(a => new AdminAuditLogDto(
            a.Id,
            a.Timestamp,
            a.AdminUserId,
            a.AdminUserName,
            a.ActionType.ToString(),
            a.TargetUserId,
            a.TargetUserName,
            a.Details,
            a.IpAddress,
            a.EntityType,
            a.EntityId)).ToList();

        return new PaginatedResult<AdminAuditLogDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}
