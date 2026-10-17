using System.Text;

using Hexalith.ChatBot.Server.Gateway;

using Microsoft.AspNetCore.Http;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed class CommandOperationConcurrencyAdapterTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"expectedRevision\":null}")]
    [InlineData("{\"expectedRevision\":-1}")]
    [InlineData("{\"expectedRevision\":1.2}")]
    [InlineData("{\"expectedRevision\":\"0\"}")]
    [InlineData("{\"expectedRevision\":0,\"expectedRevision\":1}")]
    [InlineData("{\"expectedRevision\":0,\"decisionSlotId\":\"\"}")]
    [InlineData("{\"expectedRevision\":0,\"syntheticEnabled\":true}")]
    public async Task MissingMalformedOrNegativeCanonicalGuardShouldRejectBeforeAdmission(string guard)
    {
        DefaultHttpContext context = Context(guard);
        (Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest? request, _) = await CommandSubmissionContractAdapter.ReadAsync(context, TestContext.Current.CancellationToken);
        request.ShouldBeNull();
    }

    [Theory]
    [InlineData("null", "{\"expectedRevision\":0}")]
    [InlineData("{\"expectedRevision\":-1}", "{\"expectedRevision\":0}")]
    [InlineData("{\"expectedRevision\":0}", "null")]
    [InlineData("{\"expectedRevision\":0}", "{\"expectedRevision\":-1}")]
    [InlineData("{\"expectedRevision\":0}", "{\"expectedRevision\":1}")]
    [InlineData("{\"expectedRevision\":0}", "{\"expectedRevision\":0}")]
    public async Task DuplicateRootCanonicalGuardsShouldRejectBeforeAdmission(string first, string second)
    {
        DefaultHttpContext context = Context(first + ",\"concurrency\":" + second);
        var parsed = await CommandSubmissionContractAdapter.ReadAsync(context, TestContext.Current.CancellationToken);
        parsed.Request.ShouldBeNull();
    }

    [Theory]
    [InlineData("\"decision slot\"")]
    [InlineData("\"decision/slot\"")]
    [InlineData("241-characters")]
    [InlineData("1")]
    [InlineData("null")]
    public async Task CanonicalDecisionSlotShouldRejectTokensOutsidePublishedBounds(string slot)
    {
        string token = slot == "241-characters" ? System.Text.Json.JsonSerializer.Serialize(new string('x', 241)) : slot;
        var parsed = await CommandSubmissionContractAdapter.ReadAsync(Context("{\"expectedRevision\":0,\"decisionSlotId\":" + token + "}"), TestContext.Current.CancellationToken);
        parsed.Request.ShouldBeNull();
    }

    [Fact]
    public async Task CanonicalZeroRevisionShouldParseAndOmittingWholeGuardShouldPreserveLegacyDecoding()
    {
        var canonical = await CommandSubmissionContractAdapter.ReadAsync(Context("{\"expectedRevision\":0,\"decisionSlotId\":\"decision:alpha\"}"), TestContext.Current.CancellationToken);
        canonical.Request.ShouldNotBeNull().Concurrency.ShouldNotBeNull().ExpectedRevision.ShouldBe(0);
        canonical.Request!.Concurrency!.DecisionSlotId.ShouldBe("decision:alpha");
        var legacy = await CommandSubmissionContractAdapter.ReadAsync(Context(null), TestContext.Current.CancellationToken);
        legacy.Request.ShouldNotBeNull().Concurrency.ShouldBeNull();
    }

    private static DefaultHttpContext Context(string? concurrency)
    {
        string body = "{\"commandId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAV\",\"commandType\":\"RecordGovernedNote\",\"command\":{\"noteId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAX\"},\"requestSchemaVersion\":\"v1\"" +
            (concurrency is null ? "}" : ",\"concurrency\":" + concurrency + "}");
        DefaultHttpContext context = new();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context;
    }
}
