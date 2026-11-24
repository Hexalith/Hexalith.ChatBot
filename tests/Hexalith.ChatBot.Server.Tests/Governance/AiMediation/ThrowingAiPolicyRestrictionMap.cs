using System.Collections;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Tests.Governance.AiMediation;

/// <summary>Simulates a provider restriction map that fails when snapshotted.</summary>
internal sealed class ThrowingAiPolicyRestrictionMap : IReadOnlyDictionary<AiActionRiskActionClass, bool>
{
    public int Count => 1;
    public IEnumerable<AiActionRiskActionClass> Keys => throw new NotSupportedException();
    public IEnumerable<bool> Values => throw new NotSupportedException();

    public bool this[AiActionRiskActionClass key] => throw new NotSupportedException();

    public bool ContainsKey(AiActionRiskActionClass key) => throw new NotSupportedException();

    public bool TryGetValue(AiActionRiskActionClass key, out bool value) => throw new NotSupportedException();

    public IEnumerator<KeyValuePair<AiActionRiskActionClass, bool>> GetEnumerator()
        => throw new IOException("restricted-policy-map-detail");

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
