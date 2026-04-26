namespace Onboarding.Application.Admin.DTOs;

/// <summary>
/// Request body for PUT /api/admin/users/{id} (ADMIN-03).
/// </summary>
public sealed record UpdateUserRequest(
    string RazaoSocial,
    string Email,
    string Phone);