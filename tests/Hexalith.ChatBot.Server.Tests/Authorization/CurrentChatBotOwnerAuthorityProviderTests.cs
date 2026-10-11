using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Tests.TrustedAuthority;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.Extensions.Configuration;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Authorization;

public sealed class CurrentChatBotOwnerAuthorityProviderTests
{
    [Fact]
    public async Task BoundProductionProviderResolvesCurrentMachineGrantAndRevocationDeniesAdmission()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using RSA key = RSA.Create(2048);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
        }).Build();
        string authorityKey = ChatBotAuthorityGrantKey.For("tenant-alpha", "client-alpha", "client-alpha",
            nameof(RecordGovernedNote), "service-grant", "service", ChatBotSurfaceOrigin.Api);
        var serviceGrant = new ServiceClientGrant("grant-alpha", "tenant-alpha", "client-alpha",
            ServiceClientClass.BackgroundWorker, [nameof(RecordGovernedNote)], [],
            ChatBotSurfaceOrigin.Api, now.AddMinutes(15), false, ["tenant-alpha"], "v1");
        var granted = new ChatBotAuthorityGranted("grant-alpha", "tenant-alpha", "client-alpha",
            "client-alpha", nameof(RecordGovernedNote), "service-grant", "service",
            ChatBotSurfaceOrigin.Api, serviceGrant.ExpiresAt, serviceGrant);
        var stream = new MachineGrantStream(authorityKey, granted);
        using HttpClient http = new(stream) { BaseAddress = new Uri("http://eventstore.local/") };
        var clock = new SystemClock();
        var gateway = new ChatBotBoundOwnerStreamGateway(http, new Issuer(),
            new ChatBotStreamReadProofSource(configuration, clock));
        var provider = new CurrentChatBotOwnerAuthorityProvider(gateway, clock, new ChatBotAuthorityCatalog());
        var authorizer = new ChatBotRequestAuthorizer(new ChatBotAuthorityCatalog(), provider, clock,
            new ServiceClientGrantProjectionCache(clock));
        ChatBotRequestContextResolver.TryResolve(
            TrustedAuthorityFixture.Principal(actorClass: "service", subject: "client-alpha"),
            ChatBotSurfaceOrigin.Api, out ChatBotRequestContext? context, out _).ShouldBeTrue();

        ServiceClientGrant? resolved = await authorizer.ResolveServiceGrantAsync(context!,
            nameof(RecordGovernedNote), false, true, TestContext.Current.CancellationToken);
        resolved.ShouldNotBeNull();
        resolved.GrantId.ShouldBe("grant-alpha");
        stream.Revoked = true;

        (await authorizer.ResolveServiceGrantAsync(context!, nameof(RecordGovernedNote), false, true,
            TestContext.Current.CancellationToken)).ShouldBeNull();
        stream.Reads.ShouldBeGreaterThan(0);
    }

    private sealed class SystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class Issuer : IWorkloadAssertionIssuer
    {
        public bool CanBindResources => true;

        public ValueTask<string?> IssueAsync(WorkloadAssertionRequest request, CancellationToken cancellationToken = default)
        {
            request.Audience.ShouldBe("eventstore");
            request.Operation.ShouldBe("eventstore:stream-read");
            return ValueTask.FromResult<string?>("test-workload-assertion");
        }
    }

    private sealed class MachineGrantStream(string authorityKey, ChatBotAuthorityGranted grant) : HttpMessageHandler
    {
        public bool Revoked { get; set; }

        public int Reads { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri!.AbsolutePath.ShouldBe("/api/v1/streams/read/workload");
            request.Headers.Contains("X-Hexalith-Bound-Stream-Read-Proof").ShouldBeTrue();
            request.Headers.Contains(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).ShouldBeTrue();
            StreamReadRequest read = (await request.Content!.ReadFromJsonAsync<StreamReadRequest>(cancellationToken)
                .ConfigureAwait(false)).ShouldNotBeNull();
            read.Tenant.ShouldBe("tenant-alpha");
            read.Domain.ShouldBe("chatbot");
            read.AggregateId.ShouldBe(authorityKey);
            Reads++;

            StreamReadEvent[] events =
            [
                Event(1, grant),
                .. (Revoked ? [Event(2, new ChatBotAuthorityRevoked(grant.GrantId))] : Array.Empty<StreamReadEvent>()),
            ];
            long head = events.Length;
            bool headRead = read.ToSequence == 0;
            StreamReadEvent[] pageEvents = headRead ? [] : events;
            var page = new StreamReadPage(read.Tenant, read.Domain, read.AggregateId, pageEvents,
                new StreamReadMetadata(read.FromSequence, read.ToSequence,
                    headRead ? null : head, head, pageEvents.Length, false, null));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(page) };
        }

        private static StreamReadEvent Event(long sequence, object value)
            => new(sequence, value.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(value), "json", 1,
                $"message-{sequence}", null, null, DateTimeOffset.UtcNow, "issuer");
    }
}
