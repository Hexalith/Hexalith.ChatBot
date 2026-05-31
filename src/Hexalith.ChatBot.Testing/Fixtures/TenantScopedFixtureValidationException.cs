namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Raised when a tenant-scoped fixture manifest violates the Story 1.13 schema contract.
/// </summary>
public sealed class TenantScopedFixtureValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantScopedFixtureValidationException"/> class.
    /// </summary>
    /// <param name="message">The metadata-only validation message.</param>
    public TenantScopedFixtureValidationException(string message)
        : base(message)
    {
    }
}
