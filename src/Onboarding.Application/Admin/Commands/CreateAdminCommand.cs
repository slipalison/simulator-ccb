using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;
using System.Security.Cryptography;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command to create a new admin user with a temporary password.
/// </summary>
public sealed record CreateAdminCommand(string FullName, string Email, string CreatorSub, string CreatorEmail, string? IpAddress);

/// <summary>
/// Result returned after successfully creating an admin user.
/// The temporary password is ONLY in this response — never logged or stored.
/// </summary>
public sealed record CreateAdminResult(Guid AdminId, string TemporaryPassword);

public sealed class CreateAdminCommandHandler : ICommandHandler<CreateAdminCommand, CreateAdminResult>
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAdminAuditLogRepository _adminAuditLogRepository;

    public CreateAdminCommandHandler(IKeycloakUserService keycloakUserService, IAdminAuditLogRepository adminAuditLogRepository)
    {
        _keycloakUserService = keycloakUserService;
        _adminAuditLogRepository = adminAuditLogRepository;
    }

    public async Task<CreateAdminResult> HandleAsync(CreateAdminCommand command, CancellationToken ct = default)
    {
        // Check if email already exists
        var existing = await _keycloakUserService.GetUserByEmailAsync(command.Email, ct);
        if (existing != null)
            throw new InvalidOperationException($"An user with email '{command.Email}' already exists.");

        // Generate cryptographically random temporary password
        var temporaryPassword = GenerateTemporaryPassword();

        // Create admin user in Keycloak with temporary password and admin role
        var keycloakUserId = await _keycloakUserService.CreateAdminUserAsync(
            command.Email,
            temporaryPassword,
            command.FullName,
            ct);

        var adminId = Guid.Parse(keycloakUserId);

        // Create audit log entry
        var auditLog = AdminAuditLog.Create(
            Guid.Parse(command.CreatorSub),
            command.CreatorEmail,
            ActionType.AdminCreated,
            targetUserId: adminId,
            targetUserName: command.FullName,
            details: $"{{\"email\": \"{command.Email}\"}}",
            ipAddress: command.IpAddress);

        await _adminAuditLogRepository.AddAsync(auditLog, ct);
        await _adminAuditLogRepository.SaveChangesAsync(ct);

        return new CreateAdminResult(adminId, temporaryPassword);
    }

    /// <summary>
    /// Generates a cryptographically random temporary password meeting Keycloak policy:
    /// - 14 characters
    /// - Guaranteed: 2 uppercase, 2 lowercase, 2 digits, 2 special chars
    /// - Remaining 6 chars randomly selected from all categories
    /// </summary>
    private static string GenerateTemporaryPassword()
    {
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string specialChars = "!@#$%^&*";

        var chars = new char[14];

        // Guaranteed characters
        chars[0] = GetRandomChar(upperChars);
        chars[1] = GetRandomChar(upperChars);
        chars[2] = GetRandomChar(lowerChars);
        chars[3] = GetRandomChar(lowerChars);
        chars[4] = GetRandomChar(digits);
        chars[5] = GetRandomChar(digits);
        chars[6] = GetRandomChar(specialChars);
        chars[7] = GetRandomChar(specialChars);

        // Random remaining characters
        const string allChars = upperChars + lowerChars + digits + specialChars;
        for (int i = 8; i < chars.Length; i++)
        {
            chars[i] = GetRandomChar(allChars);
        }

        // Shuffle the array using Fisher-Yates with cryptographic randomness
        for (int i = chars.Length - 1; i > 0; i--)
        {
            var randomBytes = new byte[1];
            System.Security.Cryptography.RandomNumberGenerator.Create().GetBytes(randomBytes);
            int j = randomBytes[0] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static char GetRandomChar(string charSet)
    {
        var randomBytes = new byte[1];
        System.Security.Cryptography.RandomNumberGenerator.Create().GetBytes(randomBytes);
        return charSet[randomBytes[0] % charSet.Length];
    }
}
