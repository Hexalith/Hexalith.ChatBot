using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Contracts;

/// <summary>Immutable original metadata; provenance is independent of command equivalence.</summary>
public sealed record CommandOperationIdentity
{
    /// <summary>Creates immutable server-bound metadata for the caller command ULID.</summary>
    public CommandOperationIdentity(string tenantId, string operationId, string commandType, string actorId,
        ChatBotSurfaceOrigin origin, IReadOnlyList<string> resourceIds, DateTimeOffset createdAt,
        string correlationId, string operationClass = "command-execution", string equivalenceVersion = "v1",
        string? predecessorOperationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationClass);
        ArgumentNullException.ThrowIfNull(resourceIds);
        ChatBotOperationId predecessor = default;
        if (!IsValidActorId(actorId) || !ChatBotOperationId.TryParse(operationId, out ChatBotOperationId id) ||
            !ChatBotCorrelationId.TryParse(correlationId, out ChatBotCorrelationId correlation) ||
            !Enum.IsDefined(origin) || equivalenceVersion != "v1" ||
            (predecessorOperationId is not null && !ChatBotOperationId.TryParse(predecessorOperationId, out predecessor)))
        {
            throw new ArgumentException("Invalid operation metadata.");
        }
        string[] resources = resourceIds.ToArray();
        if (resources.Any(resource => !IsValidResourceId(resource)))
        {
            throw new ArgumentException("Resource identities must satisfy the published metadata bounds.", nameof(resourceIds));
        }
        TenantId = tenantId;
        OperationId = id.Value;
        CommandType = commandType;
        ActorId = actorId;
        Origin = origin;
        ResourceIds = Array.AsReadOnly(resources);
        CreatedAt = createdAt.ToUniversalTime();
        CorrelationId = correlation.Value;
        OperationClass = operationClass;
        EquivalenceVersion = equivalenceVersion;
        PredecessorOperationId = predecessorOperationId is null ? null : predecessor.Value;
    }

    /// <summary>Gets the trusted tenant.</summary>
    public string TenantId { get; }
    /// <summary>Gets the caller-issued command ULID, independent of any task.</summary>
    public string OperationId { get; }
    /// <summary>Gets the immutable imperative command type bound to the canonical input.</summary>
    public string CommandType { get; }
    /// <summary>Gets the original trusted actor.</summary>
    public string ActorId { get; }
    /// <summary>Gets the original adapter origin.</summary>
    public ChatBotSurfaceOrigin Origin { get; }
    /// <summary>Gets a defensively copied, immutable list of resource identities.</summary>
    public IReadOnlyList<string> ResourceIds { get; }
    /// <summary>Gets the original server UTC creation time.</summary>
    public DateTimeOffset CreatedAt { get; }
    /// <summary>Gets the original correlation identity.</summary>
    public string CorrelationId { get; }
    /// <summary>Gets the original operation class.</summary>
    public string OperationClass { get; }
    /// <summary>Gets the immutable semantic equivalence version.</summary>
    public string EquivalenceVersion { get; }
    /// <summary>Gets the failed attempt preceding a distinct retry.</summary>
    public string? PredecessorOperationId { get; }
    private static bool IsValidActorId(string? actorId) => IsValidMetadataToken(actorId, allowAt: true);

    private static bool IsValidResourceId(string? resourceId) => IsValidMetadataToken(resourceId, allowAt: false);

    private static bool IsValidMetadataToken(string? value, bool allowAt)
        => value is { Length: > 0 and <= 240 } && value.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is ':' or '.' or '_' or '-' || (allowAt && character == '@'));

}
