using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

using Microsoft.Extensions.Configuration;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Authorization;

public sealed class ChatBotBoundOwnerProofTests
{
    [Fact]
    public void Issued_owner_query_proof_verifies_against_eventstore_contract()
    {
        using RSA key = RSA.Create(2048);
        string privateKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey());
        string publicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:BoundStreamRead:PrivateKey"] = privateKey,
        }).Build();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var source = new ChatBotStreamReadProofSource(configuration, new FixedClock(now));
        var request = new BoundOwnerQueryRequest(new SubmitQueryRequest("system", "tenants", "tenant-alpha",
            "Hexalith.Tenants.Contracts.Queries.ResolveTenantOwnerAuthority",
            Payload: JsonSerializer.SerializeToElement(new
            {
                tenantId = "tenant-alpha",
                principalId = "actor-alpha",
                operation = "GrantChatBotAuthority",
                actorClass = "human",
                origin = "Api",
            })), "actor-alpha");
        JsonSerializerOptions wireOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };
        string digest = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(request, wireOptions)));
        string? proof = source.Issue(new StreamReadRequest(request.Query.Tenant, request.Query.Domain, digest));

        proof.ShouldNotBeNull();
        BoundOwnerQueryProof.Verify(request, proof, "chatbot", publicKey, now).ShouldBeTrue();
    }

    private sealed record FixedClock(DateTimeOffset UtcNow) : ISystemClock;
}
