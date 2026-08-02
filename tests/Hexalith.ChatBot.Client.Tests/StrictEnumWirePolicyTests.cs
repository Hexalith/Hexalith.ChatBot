using Hexalith.ChatBot.Client.Generated;

using Newtonsoft.Json;

using Shouldly;

using ContractMailboxDelegatedSenderState = Hexalith.ChatBot.Contracts.Enums.MailboxDelegatedSenderState;

namespace Hexalith.ChatBot.Client.Tests;

/// <summary>
/// Direct coverage for the strict enum wire policy on shapes the generated client does not currently emit.
/// <para>
/// The generated client has no <c>IDictionary&lt;string, TEnum&gt;</c> property today, so the dictionary hole was
/// latent rather than live — and therefore invisible to every test that goes through <c>Client</c>. Nothing guarded the
/// next regeneration. These exercise the resolver directly so the contract holds before such a property exists.
/// </para>
/// </summary>
public sealed class StrictEnumWirePolicyTests
{
    private static JsonSerializerSettings StrictSettings()
        => new() { ContractResolver = new StrictEnumContractResolver() };

    [Fact]
    public void DictionaryValuedEnumsRejectIntegerOrdinals()
    {
        // A dictionary's IEnumerable<> element type is KeyValuePair<,>, never the enum, so this property kept NSwag's
        // permissive ItemConverterType while its scalar and array siblings rejected ordinals.
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<DictionaryCarrier>(
            """{"states":{"primary":1}}""",
            StrictSettings()));
    }

    [Fact]
    public void DictionaryValuedEnumsAcceptNamedWireValues()
    {
        DictionaryCarrier? carrier = JsonConvert.DeserializeObject<DictionaryCarrier>(
            """{"states":{"primary":"delegated"}}""",
            StrictSettings());

        carrier.ShouldNotBeNull().States.ShouldNotBeNull()["primary"]
            .ShouldBe(ContractMailboxDelegatedSenderState.Delegated);
    }

    [Fact]
    public void NullableEnumsRejectAnEmptyStringRatherThanReadingItAsAbsent()
    {
        // The base StringEnumConverter maps "" to null for a nullable enum, so a malformed value arrived as a missing
        // one and the caller could not tell it from a value the server never sent.
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<ScalarCarrier>(
            """{"state":""}""",
            StrictSettings()));
    }

    [Fact]
    public void NullableEnumsStillAcceptAnExplicitNull()
    {
        ScalarCarrier? carrier = JsonConvert.DeserializeObject<ScalarCarrier>(
            """{"state":null}""",
            StrictSettings());

        carrier.ShouldNotBeNull().State.ShouldBeNull();
    }

    private sealed class DictionaryCarrier
    {
        [JsonProperty("states")]
        public IDictionary<string, ContractMailboxDelegatedSenderState>? States { get; set; }
    }

    private sealed class ScalarCarrier
    {
        [JsonProperty("state")]
        public ContractMailboxDelegatedSenderState? State { get; set; }
    }
}
