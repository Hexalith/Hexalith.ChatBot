using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class ServiceClientGrantContractTests
{
    [Theory]
    [InlineData(ServiceClientClass.CliAutomation, "cli-automation")]
    [InlineData(ServiceClientClass.McpTool, "mcp-tool")]
    [InlineData(ServiceClientClass.BackgroundWorker, "background-worker")]
    [InlineData(ServiceClientClass.MailboxIngestion, "mailbox-ingestion")]
    [InlineData(ServiceClientClass.AuditProjection, "audit-projection")]
    [InlineData(ServiceClientClass.AiActionExecution, "ai-action-execution")]
    public static void ServiceClientClassWireTokensShouldRoundTrip(ServiceClientClass clientClass, string token)
    {
        ServiceClientClasses.ToWireValue(clientClass).ShouldBe(token);
        ServiceClientClasses.TryFromWireValue(token, out ServiceClientClass parsed).ShouldBeTrue();
        parsed.ShouldBe(clientClass);
    }

    [Fact]
    public static void ServiceClientGrantShouldSerializeMetadataOnlyEvidence()
    {
        ServiceClientGrant grant = new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "tenant-alpha",
            "cli-automation-client",
            ServiceClientClass.CliAutomation,
            [nameof(Hexalith.ChatBot.Contracts.Commands.RecordGovernedNote)],
            ["GetOperationStatus"],
            ChatBotSurfaceOrigin.Cli,
            new DateTimeOffset(2026, 6, 1, 23, 59, 0, TimeSpan.Zero),
            false,
            ["notes.write"],
            "command-set-v1",
            "actor-alpha",
            "oauth-proof-01ARZ3NDEKTSV4RRFFQ69G5FAV");

        string json = JsonSerializer.Serialize(grant, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("cli-automation-client");
        json.ShouldContain("RecordGovernedNote");
        json.ShouldContain("oauth-proof-01ARZ3NDEKTSV4RRFFQ69G5FAV");
        json.ShouldNotContain("bearer", Case.Insensitive);
        json.ShouldNotContain("accessToken", Case.Insensitive);
        json.ShouldNotContain("refreshToken", Case.Insensitive);
        json.ShouldNotContain("clientSecret", Case.Insensitive);
        json.ShouldNotContain("rawClaims", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
    }

    [Fact]
    public static void ServiceClientGrantContractsShouldNotExposeSecretBearingProperties()
    {
        string[] blockedNameFragments = ["Secret", "Token", "RawClaim", "RawJwt", "ProviderPayload", "Assertion"];
        Type[] contractTypes = [typeof(ServiceClientGrant), typeof(ServiceClientGrantEvidence)];

        foreach (Type contractType in contractTypes)
        {
            string[] propertyNames = contractType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(static property => property.Name)
                .ToArray();

            foreach (string blocked in blockedNameFragments)
            {
                propertyNames.ShouldNotContain(name => name.Contains(blocked, StringComparison.Ordinal), contractType.Name);
            }
        }
    }
}
