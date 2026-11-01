using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Authentication;

/// <summary>Checks the early denial boundary without registering any admission, dispatch or protected-read services.</summary>
public sealed class ChatBotApplicationBoundaryResultHandlerTests
{
    [Fact]
    public async Task StalledMetadataReadMustExpireWithOneSafeFactAndTypedDenial()
    {
        BoundaryAuditWriter audit = new();
        using ServiceProvider services = Services(audit);
        DefaultHttpContext context = Context(services);
        TaskCompletionSource<int> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken readToken = default;
        using BoundaryRequestBodyStream body = new(token =>
        {
            readToken = token;
            return pending.Task; // Deliberately ignores cancellation to exercise the bounded wait as well.
        });
        context.Request.Body = body;
        try
        {
            await DenyAsync(context).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

            readToken.IsCancellationRequested.ShouldBeTrue();
            context.RequestAborted.IsCancellationRequested.ShouldBeFalse();
            AssertSafeMetadataDenial(context, audit, AuditMetadata.UnknownCommandName);
        }
        finally
        {
            pending.TrySetResult(0);
        }
    }

    [Theory]
    [InlineData("transport")]
    [InlineData("framing")]
    [InlineData("invalid-data")]
    public async Task BodyTransportAndFramingFailureMustRetainOneSafeFactAndTypedDenial(string failureKind)
    {
        BoundaryAuditWriter audit = new();
        using ServiceProvider services = Services(audit);
        DefaultHttpContext context = Context(services);
        Exception failure = failureKind switch
        {
            "framing" => new BadHttpRequestException("restricted-body-framing", StatusCodes.Status400BadRequest),
            "invalid-data" => new InvalidDataException("restricted-body-data"),
            _ => new IOException("restricted-body-transport"),
        };
        using BoundaryRequestBodyStream body = new(_ => Task.FromException<int>(failure));
        context.Request.Body = body;

        await DenyAsync(context).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        AssertSafeMetadataDenial(context, audit, AuditMetadata.UnknownCommandName);
    }

    [Theory]
    [InlineData("{\"commandType\":\"\\uD800\"}", "unknown_command")]
    [InlineData("{\"commandType\":\"\\uDC00\"}", "unknown_command")]
    [InlineData("{\"\\uD800\":\"x\",\"commandType\":\"RecordGovernedNote\"}", "unknown_command")]
    [InlineData("{\"\\uDC00\":\"x\",\"commandType\":\"RecordGovernedNote\"}", "unknown_command")]
    [InlineData("{\"commandType\":\"alice@example.com\"}", "unknown_command")]
    [InlineData("{\"commandType\":\"AliceSmith\"}", "unknown_command")]
    [InlineData("{\"commandType\":\"recordGovernedNote\"}", "unknown_command")]
    [InlineData("{\"commandType\":\"RecordGovernedNote\"}", "RecordGovernedNote")]
    public async Task MalformedStringsAndNoncanonicalMetadataMustNotDisclosePersonalData(string json, string expectedName)
    {
        BoundaryAuditWriter audit = new();
        using ServiceProvider services = Services(audit);
        DefaultHttpContext context = Context(services);
        using MemoryStream body = new(Encoding.UTF8.GetBytes(json));
        context.Request.Body = body;

        await DenyAsync(context);

        AssertSafeMetadataDenial(context, audit, expectedName);
    }

    [Fact]
    public async Task RequestAbortedDuringMetadataReadMustPropagateWithoutAudit()
    {
        BoundaryAuditWriter audit = new();
        using ServiceProvider services = Services(audit);
        DefaultHttpContext context = Context(services);
        using CancellationTokenSource aborted = new();
        context.RequestAborted = aborted.Token;
        TaskCompletionSource<int> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using BoundaryRequestBodyStream body = new(_ =>
        {
            aborted.Cancel();
            return pending.Task;
        });
        context.Request.Body = body;
        try
        {
            _ = await Should.ThrowAsync<OperationCanceledException>(() => DenyAsync(context)
                .WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
            audit.Facts.ShouldBeEmpty();
        }
        finally
        {
            pending.TrySetResult(0);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OversizedDeniedBodyMustStayBoundedWithOrWithoutContentLength(bool declaredLength)
    {
        BoundaryAuditWriter audit = new();
        using ServiceProvider services = Services(audit);
        DefaultHttpContext context = Context(services);
        using MemoryStream body = new(Encoding.UTF8.GetBytes("{\"commandType\":\"RecordGovernedNote\",\"command\":\"" + new string('x', 32 * 1024) + "\"}"));
        context.Request.Body = body;
        context.Request.ContentLength = declaredLength ? body.Length : null;

        await DenyAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        audit.Facts.ShouldHaveSingleItem().CommandType.ShouldBe(AuditMetadata.UnknownCommandName);
        body.Position.ShouldBe(declaredLength ? 0 : (16 * 1024) + 1);
    }

    [Fact]
    public async Task CancelledDenialMustPropagateCancellationWithoutAuditOrEndpointEffects()
    {
        BoundaryAuditWriter audit = new();
        using ServiceProvider services = Services(audit);
        DefaultHttpContext context = Context(services);
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();
        context.RequestAborted = cancelled.Token;

        _ = await Should.ThrowAsync<OperationCanceledException>(() => DenyAsync(context));
        audit.Facts.ShouldBeEmpty();
    }

    [Fact]
    public async Task AuditBackendFailureMustPropagateWithoutRetryOrEndpointEffects()
    {
        InvalidOperationException failure = new("audit-backend-failure");
        BoundaryAuditWriter audit = new(failure);
        using ServiceProvider services = Services(audit);
        DefaultHttpContext context = Context(services);

        (await Should.ThrowAsync<InvalidOperationException>(() => DenyAsync(context))).ShouldBeSameAs(failure);
        audit.Facts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task DeniedReadMustNotCreateACommandAuditFact()
    {
        BoundaryAuditWriter audit = new();
        using ServiceProvider services = Services(audit);
        DefaultHttpContext context = Context(services, "/api/v1/operations/{operationId}");
        context.Request.Method = HttpMethods.Get;

        await DenyAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        audit.Facts.ShouldBeEmpty();
    }

    private static ServiceProvider Services(BoundaryAuditWriter audit)
        => new ServiceCollection().AddLogging()
            .AddSingleton<IAuditWriter>(audit)
            .AddSingleton<IChatBotProblemDetailsFactory>(new ChatBotProblemDetailsFactory(new CoarseUserFacingRedactionStage(), new InMemoryUserFacingMessageTelemetry()))
            .BuildServiceProvider();

    private static void AssertSafeMetadataDenial(DefaultHttpContext context, BoundaryAuditWriter audit, string expectedName)
    {
        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        ChatBotAuthorizationFailureAuditFact fact = audit.Facts.ShouldHaveSingleItem();
        fact.CommandType.ShouldBe(expectedName);
        fact.TenantId.ShouldBe("unavailable");
        fact.ActorId.ShouldBe("anonymous");
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
        string response = Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
        using JsonDocument problem = JsonDocument.Parse(response);
        problem.RootElement.GetProperty("code").GetString().ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
        problem.RootElement.GetProperty("correlationId").GetString().ShouldBe(fact.CorrelationId);
        context.Response.Headers["X-Correlation-Id"].ToString().ShouldBe(fact.CorrelationId);
        string output = response + JsonSerializer.Serialize(fact);
        foreach (string sentinel in new[] { "alice@example.com", "AliceSmith", "restricted-body", "\\uD800", "\\uDC00" })
        {
            output.ShouldNotContain(sentinel);
        }
    }

    private static DefaultHttpContext Context(IServiceProvider services, string route = "/api/v1/commands")
    {
        DefaultHttpContext context = new() { RequestServices = services };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream("{}"u8.ToArray());
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new RouteEndpoint(_ => throw new InvalidOperationException("Denied endpoint must not execute."),
            RoutePatternFactory.Parse(route), 0,
            new EndpointMetadataCollection(new AuthorizeAttribute(ChatBotApplicationBoundaryPolicy.Name)), "boundary-test"));
        return context;
    }

    private static Task DenyAsync(HttpContext context)
        => new ChatBotApplicationBoundaryResultHandler().HandleAsync(
            _ => throw new InvalidOperationException("Denied request must not advance."), context,
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(), PolicyAuthorizationResult.Challenge());
}
