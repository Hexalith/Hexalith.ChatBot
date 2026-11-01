using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Routing;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Routing;

/// <summary>Checks the static owner inventory against compiled behavior and complete payload binding.</summary>
public sealed class ChatBotCatalogTests
{
    /// <summary>No reflected capability is generated at runtime; the explicit owner list must match every real overload.</summary>
    [Fact]
    public void DeclarationsMustMatchTheActualAggregateAndQueryHandlerInventory()
    {
        Type[] actual = typeof(GovernedOperationAggregate).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "Handle").Select(method => method.GetParameters()[0].ParameterType).Distinct().ToArray();
        ChatBotCatalogDeclarations.CommandTypes.Count.ShouldBe(64);
        ChatBotCatalogDeclarations.CommandTypes.OrderBy(type => type.FullName)
            .ShouldBe(actual.OrderBy(type => type.FullName));
        IReadOnlyList<RouteContractDeclaration> queries = DomainCatalogCapabilities.Queries(typeof(GovernedOperationAggregate).Assembly);
        queries.Count.ShouldBe(8);
        queries.Select(RouteCatalogCodec.ComputeDeclarationDigest).Order()
            .ShouldBe(ChatBotCatalogDeclarations.Queries.Select(RouteCatalogCodec.ComputeDeclarationDigest).Order());
        ChatBotCatalogDeclarations.Routes.Count.ShouldBe(73);
        CompiledRouteCatalogDeclarations.Read([typeof(GovernedOperationAggregate).Assembly]).Select(route => route.RouteId).Order()
            .ShouldBe(ChatBotCatalogDeclarations.Routes.Select(route => route.RouteId).Order());
        foreach (RouteContractDeclaration route in ChatBotCatalogDeclarations.Routes) { RouteCatalogValidator.ValidateDeclaration(route); }
        ChatBotCatalogDeclarations.Projections.ShouldHaveSingleItem().Execution!.ProjectionOutputs.ShouldBe(["chatbot"]);
        ChatBotCatalogDeclarations.Operations.Count.ShouldBe(73);
        ChatBotCatalogDeclarations.Operations.ShouldAllBe(operation => operation.Ingress == "internal" && operation.Eligibility == "UI-only");
    }

    /// <summary>Every actual payload has one typed adapter, and extra payload members cannot silently collide.</summary>
    [Fact]
    public void OwnerAdaptersMustBindAllPayloadFieldsWithIndependentSdkRetention()
    {
        IReadOnlyList<IDeclaredIdempotencyIntentAdapter> adapters = ChatBotCatalogAdapters.Create();
        adapters.Count.ShouldBe(64);
        adapters.Select(adapter => adapter.CommandType).Order().ShouldBe(ChatBotCatalogDeclarations.CommandTypes.Select(type => type.Name).Order());
        adapters.ShouldAllBe(adapter => adapter.RetentionTier == IdempotencyReplayRetentionTier.Mutation);
        IDeclaredIdempotencyIntentAdapter note = adapters.Single(adapter => adapter.CommandType == nameof(RecordGovernedNote));
        byte[] first = JsonSerializer.SerializeToUtf8Bytes(new { NoteId = "note-001", AdditionalMember = "first" });
        byte[] second = JsonSerializer.SerializeToUtf8Bytes(new { NoteId = "note-001", AdditionalMember = "second" });
        IdempotencyCanonicalIntent Intent(byte[] payload, string? subject = "actor-alpha") => note.CreateIntent(
            new(nameof(RecordGovernedNote), "tenant-alpha", "chatbot", "note-001", payload,
                new Dictionary<string, string> { ["acting-subject"] = "caller-supplied-actor" }) { AuthenticatedSubject = subject });
        Intent(first).SemanticPayload.ShouldNotBe(Intent(second).SemanticPayload);
        Intent(first, "actor-alpha").SemanticPayload.ShouldNotBe(Intent(first, "actor-beta").SemanticPayload);
        using JsonDocument fields = JsonDocument.Parse(Intent(first, "actor-alpha").SemanticPayload);
        fields.RootElement.GetProperty("acting-subject").GetString().ShouldBe("actor-alpha");
        Should.Throw<InvalidOperationException>(() => Intent(first, null));
        Should.Throw<InvalidOperationException>(() => Intent(first, " "));
        adapters.ShouldAllBe(adapter => adapter.Declaration.RequiresAuthenticatedSubject &&
            adapter.Declaration.DescriptorSchema.Fields.Any(field => field.Name == "acting-subject" && field.Type == "string"));
    }
}
