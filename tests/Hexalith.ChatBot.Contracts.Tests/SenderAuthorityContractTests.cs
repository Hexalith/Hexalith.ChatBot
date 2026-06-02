using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class SenderAuthorityContractTests
{
    [Theory]
    [InlineData(SenderAuthorityClass.DraftOnly, "draft-only")]
    [InlineData(SenderAuthorityClass.AuthenticatedUserSend, "authenticated-user send")]
    [InlineData(SenderAuthorityClass.SharedMailboxSend, "shared-mailbox send")]
    [InlineData(SenderAuthorityClass.SendOnBehalf, "send-on-behalf")]
    [InlineData(SenderAuthorityClass.ApprovedServiceSend, "approved service-send")]
    public static void SenderAuthorityClassWireTokensShouldRoundTrip(SenderAuthorityClass authorityClass, string token)
    {
        SenderAuthorityClasses.ToWireValue(authorityClass).ShouldBe(token);
        SenderAuthorityClasses.TryFromWireValue(token, out SenderAuthorityClass parsed).ShouldBeTrue();
        parsed.ShouldBe(authorityClass);
    }

    [Theory]
    [InlineData("DRAFT-ONLY", SenderAuthorityClass.DraftOnly)]
    [InlineData(" authenticated-user send ", SenderAuthorityClass.AuthenticatedUserSend)]
    [InlineData("shared-mailbox send", SenderAuthorityClass.SharedMailboxSend)]
    [InlineData("SEND-ON-BEHALF", SenderAuthorityClass.SendOnBehalf)]
    [InlineData("approved service-send", SenderAuthorityClass.ApprovedServiceSend)]
    public static void SenderAuthorityClassParsingShouldBeTolerant(string token, SenderAuthorityClass expected)
    {
        SenderAuthorityClasses.TryFromWireValue(token, out SenderAuthorityClass parsed).ShouldBeTrue();
        parsed.ShouldBe(expected);
    }

    [Fact]
    public static void SenderAuthorityConflictReasonsShouldExposeFiniteTokens()
    {
        SenderAuthorityConflictReasons.All.ShouldBe(
            [
                ChatBotDisabledActionReasons.PolicyBlocked,
                SenderAuthorityConflictReasons.DelegationMismatch,
                SenderAuthorityConflictReasons.MembershipRevoked,
                SenderAuthorityConflictReasons.ApprovalMissing,
            ],
            ignoreOrder: false);
    }

    [Fact]
    public static void SenderAuthorityClassificationResultShouldSerializeMetadataOnly()
    {
        SenderAuthorityClassificationResult result = new(
            SenderAuthorityClass.SendOnBehalf,
            "requester:actor-alpha",
            "mailbox:shared-alpha",
            null,
            "principal-for:owner-alpha",
            "approval:approval-alpha",
            "policy-snapshot:policy-alpha",
            "fresh",
            [
                "sender-authority:send-on-behalf",
                "principal-for:owner-alpha",
                "policy-snapshot:policy-alpha",
            ],
            SenderAuthorityConflictReasons.DelegationMismatch);

        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("send-on-behalf");
        json.ShouldContain("requester:actor-alpha");
        json.ShouldContain("principal-for:owner-alpha");
        json.ShouldContain(SenderAuthorityConflictReasons.DelegationMismatch);
        AssertMetadataOnly(json);
    }

    [Fact]
    public static void SenderAuthorityPublicContractsShouldNotExposeSecretBearingProperties()
    {
        string[] blockedNameFragments =
        [
            "AccessToken",
            "RefreshToken",
            "RawClaim",
            "RawJwt",
            "ProviderPayload",
            "Header",
            "Body",
            "RecipientDisplayName",
            "ProjectName",
        ];
        Type[] contractTypes = [typeof(SenderAuthorityClassificationResult)];

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

    private static void AssertMetadataOnly(string json)
    {
        string[] blocked =
        [
            "bearer",
            "accessToken",
            "refreshToken",
            "rawClaims",
            "providerPayload",
            "internetMessageHeaders",
            "messageBody",
            "recipient display",
            "Project Apollo",
            "Graph response",
        ];

        foreach (string marker in blocked)
        {
            json.ShouldNotContain(marker, Case.Insensitive);
        }
    }
}
