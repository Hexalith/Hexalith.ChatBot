namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>A closed operation row, independent of caller authorization flags.</summary>
/// <param name="Operation">The exact command or query name.</param>
/// <param name="IsQuery">Whether this row represents a read.</param>
/// <param name="AdminScope">The exact human administration scope, if required.</param>
/// <param name="ResourceProperty">The payload property carrying the primary resource.</param>
/// <param name="RequiresProject">Whether current Projects authority is mandatory.</param>
/// <param name="RequiresParties">Whether current Parties evidence is mandatory.</param>
internal sealed record ChatBotAuthorityRequirement(string Operation, bool IsQuery, string? AdminScope, string? ResourceProperty, bool RequiresProject, bool RequiresParties);
