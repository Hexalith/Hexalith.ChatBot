using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

/// <summary>
/// The finite authority class an AI actor must hold to invoke a governed command. Modelled as a closed token
/// set (mirroring <see cref="Hexalith.ChatBot.Contracts.Enums.SenderAuthorityClass"/>) rather than a free
/// string so allowlist v1 metadata cannot smuggle an unbounded authority claim. Server-internal: not a wire
/// contract.
/// </summary>
internal enum AiActionAuthorityClass
{
    /// <summary>Read-only assistance that mutates no durable state (e.g. summarise visible context).</summary>
    ReadOnlyAssistant,

    /// <summary>Acts under the requester's delegated project authority to mutate project-scoped state.</summary>
    DelegatedProjectContributor,
}
