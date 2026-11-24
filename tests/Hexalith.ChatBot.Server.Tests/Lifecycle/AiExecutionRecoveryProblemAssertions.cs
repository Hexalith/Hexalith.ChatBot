using System.Net;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Messages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

/// <summary>Pins AI execution recovery failures to the versioned problem catalog the read routes use.</summary>
internal static class AiExecutionRecoveryProblemAssertions
{
    /// <summary>The fixed correlation identifier that makes two problem bodies byte-comparable.</summary>
    public const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    /// <summary>A read route that returns the catalog-backed safe-not-found problem for an unknown queue family.</summary>
    private const string ReadRouteSafeNotFoundUri = "/api/v1/projects/project-alpha/review-queues/unknown-queue-family";

    /// <summary>Asserts that a recovery response is byte-for-byte the read routes' safe-not-found problem.</summary>
    /// <param name="response">The recovery response.</param>
    /// <param name="client">A client that sends <see cref="CorrelationId"/> with every request.</param>
    /// <returns>A task that completes when the comparison has run.</returns>
    public static async Task ShouldBeReadRouteSafeNotFoundAsync(HttpResponseMessage response, HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(client);
        using HttpResponseMessage reference = await client.GetAsync(ReadRouteSafeNotFoundUri, TestContext.Current.CancellationToken).ConfigureAwait(true);
        reference.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.StatusCode.ShouldBe(reference.StatusCode);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        body.ShouldBe(await reference.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
        using JsonDocument problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        problem.RootElement.GetProperty("schemaVersion").GetString().ShouldBe(ChatBotMessageCatalogVersion.Current);
        problem.RootElement.GetProperty("correlationId").GetString().ShouldBe(CorrelationId);
    }

    /// <summary>Asserts the catalog-backed validation problem returned for an undecodable cursor.</summary>
    /// <param name="response">The recovery response.</param>
    /// <returns>A task that completes when the assertions have run.</returns>
    public static async Task ShouldBeValidationProblemAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
        problem.RootElement.GetProperty("code").GetString().ShouldBe(ChatBotMessageCodes.CommandContractInvalid);
        problem.RootElement.GetProperty("schemaVersion").GetString().ShouldBe(ChatBotMessageCatalogVersion.Current);
        problem.RootElement.GetProperty("correlationId").GetString().ShouldBe(CorrelationId);
    }
}
