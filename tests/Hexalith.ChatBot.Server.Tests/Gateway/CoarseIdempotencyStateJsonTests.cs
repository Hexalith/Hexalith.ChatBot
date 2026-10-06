using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Gateway.Idempotency;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed class CoarseIdempotencyStateJsonTests
{
    private static readonly DateTimeOffset AcceptedAt = new(2026, 10, 6, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public void GeneratedEnumsShouldPersistAsWireValuesAndRoundTrip()
    {
        CoarseCommandIdentityRecord record = Identity(Outcome(ChatBotMessageCode.Command_accepted, LifecycleState.Proposed));

        string json = JsonSerializer.Serialize(record, CoarseIdempotencyStateJson.Options);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement prior = document.RootElement.GetProperty("priorOutcome");
        prior.GetProperty("reasonCode").GetString().ShouldBe("command_accepted");
        prior.GetProperty("lifecycleState").GetString().ShouldBe("Proposed");
        JsonElement reservation = document.RootElement.GetProperty("domainReservation");
        reservation.GetProperty("dispatchState").GetString().ShouldBe("Dispatching");
        reservation.GetProperty("preparedOutcome").GetProperty("reasonCode").GetString().ShouldBe("command_accepted");
        CoarseCommandIdentityRecord restored = JsonSerializer.Deserialize<CoarseCommandIdentityRecord>(json, CoarseIdempotencyStateJson.Options)
            .ShouldNotBeNull();
        AssertSameOutcome(restored.PriorOutcome.ShouldNotBeNull(), record.PriorOutcome!);
        AssertSameOutcome(restored.DomainReservation.ShouldNotBeNull().PreparedOutcome.ShouldNotBeNull(), record.DomainReservation!.PreparedOutcome!);
        restored.DomainReservation.DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
    }

    [Fact]
    public void OrdinalsPersistedByTheDaprWebDefaultShouldRemainReadable()
    {
        CoarseCommandIdentityRecord record = Identity(Outcome(ChatBotMessageCode.Command_accepted, LifecycleState.Proposed));
        string legacy = JsonSerializer.Serialize(record, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using (JsonDocument document = JsonDocument.Parse(legacy))
        {
            document.RootElement.GetProperty("priorOutcome").GetProperty("reasonCode").ValueKind.ShouldBe(JsonValueKind.Number);
            document.RootElement.GetProperty("domainReservation").GetProperty("dispatchState").ValueKind.ShouldBe(JsonValueKind.Number);
        }

        CoarseCommandIdentityRecord restored = JsonSerializer.Deserialize<CoarseCommandIdentityRecord>(legacy, CoarseIdempotencyStateJson.Options)
            .ShouldNotBeNull();

        AssertSameOutcome(restored.PriorOutcome.ShouldNotBeNull(), record.PriorOutcome!);
        restored.DomainReservation.ShouldNotBeNull().DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
    }

    [Fact]
    public void EveryGeneratedReasonCodeShouldPersistAsItsEnumMemberValue()
    {
        foreach (FieldInfo field in typeof(ChatBotMessageCode).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            ChatBotMessageCode value = (ChatBotMessageCode)field.GetValue(null)!;
            string wire = field.GetCustomAttribute<EnumMemberAttribute>().ShouldNotBeNull().Value.ShouldNotBeNull();

            string json = JsonSerializer.Serialize(value, CoarseIdempotencyStateJson.Options);

            json.ShouldBe(JsonSerializer.Serialize(wire));
            JsonSerializer.Deserialize<ChatBotMessageCode>(json, CoarseIdempotencyStateJson.Options).ShouldBe(value);
        }
    }

    [Fact]
    public void NullableUndeclaredAndDictionaryKeyedEnumsShouldRoundTrip()
    {
        JsonSerializer.Serialize<ChatBotMessageCode?>(null, CoarseIdempotencyStateJson.Options).ShouldBe("null");
        JsonSerializer.Deserialize<ChatBotMessageCode?>("\"command_accepted\"", CoarseIdempotencyStateJson.Options)
            .ShouldBe(ChatBotMessageCode.Command_accepted);

        const ChatBotMessageCode undeclared = (ChatBotMessageCode)9_999;
        string number = JsonSerializer.Serialize(undeclared, CoarseIdempotencyStateJson.Options);
        number.ShouldBe("9999");
        JsonSerializer.Deserialize<ChatBotMessageCode>(number, CoarseIdempotencyStateJson.Options).ShouldBe(undeclared);

        Dictionary<LifecycleState, int> keyed = new() { [LifecycleState.Proposed] = 1, [(LifecycleState)77] = 2 };
        string dictionary = JsonSerializer.Serialize(keyed, CoarseIdempotencyStateJson.Options);
        dictionary.ShouldBe("""{"Proposed":1,"77":2}""");
        JsonSerializer.Deserialize<Dictionary<LifecycleState, int>>(dictionary, CoarseIdempotencyStateJson.Options).ShouldBe(keyed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OperationStatusRecordShouldPersistGeneratedEnumsAsWireValuesThroughTheSharedReadModelClient(bool webDefaults)
    {
        // The shared read-model DAPR client keeps the DAPR web defaults (and the SDK batch path the bare defaults), so
        // the record itself must pin the wire encoding of the generated enums it carries.
        JsonSerializerOptions shared = webDefaults ? new JsonSerializerOptions(JsonSerializerDefaults.Web) : new JsonSerializerOptions();
        CommandSubmissionResponse prior = Outcome(ChatBotMessageCode.Command_accepted, LifecycleState.Proposed);
        Hexalith.ChatBot.Server.Gateway.Status.OperationStatusRecord record =
            Hexalith.ChatBot.Server.Gateway.Status.OperationStatusRecord.Accepted("tenant-alpha", prior, false, AcceptedAt)
            with { PriorOutcome = prior };

        string json = JsonSerializer.Serialize(record, shared);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        root.GetProperty(webDefaults ? "lifecycleState" : "LifecycleState").GetString().ShouldBe("Proposed");
        JsonElement priorOutcome = root.GetProperty(webDefaults ? "priorOutcome" : "PriorOutcome");
        priorOutcome.GetProperty("reasonCode").GetString().ShouldBe("command_accepted");
        priorOutcome.GetProperty("lifecycleState").GetString().ShouldBe("Proposed");
        Hexalith.ChatBot.Server.Gateway.Status.OperationStatusRecord restored =
            JsonSerializer.Deserialize<Hexalith.ChatBot.Server.Gateway.Status.OperationStatusRecord>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                .ShouldNotBeNull();
        restored.LifecycleState.ShouldBe(LifecycleState.Proposed);
        AssertSameOutcome(restored.PriorOutcome.ShouldNotBeNull(), prior);
    }

    [Fact]
    public void OperationStatusRecordOrdinalsPersistedBeforeTheWireEncodingShouldRemainReadable()
    {
        const string legacy = """
            {"tenantId":"tenant-alpha","operationId":"01ARZ3NDEKTSV4RRFFQ69G5FAX","commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAW","lifecycleState":1,"retryCount":0,"completionStatus":"accepted-projection-pending",
            "auditStatus":"committed","safeNextActions":["none"],"terminalReason":null,"acceptedAt":"2026-10-06T08:30:00+00:00",
            "lastUpdatedAt":"2026-10-06T08:30:00+00:00","reasonCode":"command_accepted",
            "priorOutcome":{"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAY","correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAW","operationId":"01ARZ3NDEKTSV4RRFFQ69G5FAX",
            "taskId":"01ARZ3NDEKTSV4RRFFQ69G5FAX","lifecycleState":1,"acceptedAt":"2026-10-06T08:30:00+00:00","reasonCode":0,"retryEligible":false}}
            """;

        Hexalith.ChatBot.Server.Gateway.Status.OperationStatusRecord restored =
            JsonSerializer.Deserialize<Hexalith.ChatBot.Server.Gateway.Status.OperationStatusRecord>(legacy, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                .ShouldNotBeNull();

        restored.LifecycleState.ShouldBe(LifecycleState.Proposed);
        CommandSubmissionResponse prior = restored.PriorOutcome.ShouldNotBeNull();
        prior.ReasonCode.ShouldBe(ChatBotMessageCode.Command_accepted);
        prior.LifecycleState.ShouldBe(LifecycleState.Proposed);
    }

    [Theory]
    [InlineData("\"not_a_reason\"")]
    [InlineData("\"Command_Accepted_Typo\"")]
    [InlineData("true")]
    [InlineData("1.5")]
    public void UnknownOrMalformedTokensShouldFailClosed(string json)
        => Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ChatBotMessageCode>(json, CoarseIdempotencyStateJson.Options));

    private static CommandSubmissionResponse Outcome(ChatBotMessageCode reason, LifecycleState state)
        => new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            OperationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            LifecycleState = state,
            AcceptedAt = AcceptedAt,
            ReasonCode = reason,
            RetryEligible = false,
        };

    private static CoarseCommandIdentityRecord Identity(CommandSubmissionResponse outcome)
        => new(
            "tenant-alpha",
            outcome.CommandId,
            "caller-fingerprint",
            "domain-key",
            AcceptedAt,
            outcome,
            new CoarseIdempotencyRecord(
                "tenant-alpha",
                "command-execution",
                "domain-key",
                "equivalence",
                outcome.CorrelationId,
                outcome.TaskId,
                outcome.CommandId,
                "RecordGovernedNote",
                "actor-alpha",
                AcceptedAt,
                AcceptedAt.AddMinutes(1),
                null,
                ReservationId: "reservation-1",
                DispatchState: CoarseDispatchState.Dispatching,
                ReservationLeaseExpiresAt: AcceptedAt.AddMinutes(2),
                PreparedOutcome: outcome));

    private static void AssertSameOutcome(CommandSubmissionResponse actual, CommandSubmissionResponse expected)
    {
        actual.CommandId.ShouldBe(expected.CommandId);
        actual.CorrelationId.ShouldBe(expected.CorrelationId);
        actual.TaskId.ShouldBe(expected.TaskId);
        actual.OperationId.ShouldBe(expected.OperationId);
        actual.LifecycleState.ShouldBe(expected.LifecycleState);
        actual.AcceptedAt.ShouldBe(expected.AcceptedAt);
        actual.ReasonCode.ShouldBe(expected.ReasonCode);
        actual.RetryEligible.ShouldBe(expected.RetryEligible);
    }
}
