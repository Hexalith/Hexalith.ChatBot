using System.Text.Json;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

/// <summary>
/// Serialization settings of the DAPR client that persists coarse idempotency identity, domain, and receipt records.
/// </summary>
/// <remarks>
/// Those records embed the generated <c>CommandSubmissionResponse</c>, whose <c>ChatBotMessageCode</c> and
/// <c>LifecycleState</c> enums are generated with Newtonsoft <c>[EnumMember]</c> wire values. The DAPR default (web
/// System.Text.Json) persists them as ordinals, which change meaning whenever generation inserts or reorders a member.
/// The idempotency store therefore uses its own DAPR client configured with these options: wire-value strings are
/// written, and integer tokens are still accepted but interpreted with the current generated numbering, so they decode
/// correctly only for records written by the same enum generation (records persisted before a renumbering misdecode;
/// accepted while the product is pre-release). The shared host <c>DaprClient</c> keeps the
/// DAPR web defaults that the EventStore read-model batch seam requires for its canonical bytes.
/// </remarks>
internal static class CoarseIdempotencyStateJson
{
    /// <summary>The DI key of the DAPR client dedicated to coarse idempotency state.</summary>
    public const string DaprClientKey = "chatbot-coarse-idempotency-state";

    /// <summary>Gets the serializer options for coarse idempotency state.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new EnumMemberWireValueJsonConverterFactory());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
