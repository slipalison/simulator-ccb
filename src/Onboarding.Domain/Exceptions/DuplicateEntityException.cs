namespace Onboarding.Domain.Exceptions;

/// <summary>
/// Thrown when an attempt to create or register an entity conflicts with an existing
/// record identified by a unique key (CNPJ, CPF, codigo, etc.).
/// The message includes entity type and key value for observability — callers must NOT
/// propagate this directly to HTTP responses to avoid information leakage.
/// </summary>
public sealed class DuplicateEntityException : DomainException
{
    public string EntityType { get; }
    public string KeyValue { get; }

    public DuplicateEntityException(string entityType, string keyValue)
        : base($"'{entityType}' with key '{keyValue}' already exists.")
    {
        EntityType = entityType;
        KeyValue = keyValue;
    }

    public DuplicateEntityException(string message)
        : base(message)
    {
        EntityType = string.Empty;
        KeyValue = string.Empty;
    }
}