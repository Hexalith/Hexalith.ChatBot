using System.Security.Cryptography;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Hexalith.ChatBot.IntegrationTests.Recovery;
using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>Composition contracts for the retained local owner resources and complete static overrides.</summary>
[Collection(RealmTemplateMutationCollection.Name)]
public sealed class OwnerServiceTopologyTests
{
    [Fact]
    public async Task DevelopmentWithoutStaticOwnerSettingsComposesDedicatedOwnerBindings()
    {
        string mailboxSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(
                ["--environment=Development", $"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={mailboxSecret}"],
                TestContext.Current.CancellationToken).ConfigureAwait(true);
        try
        {
            ProjectResource chatbot = builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == "chatbot");
            Dictionary<string, object> environment = await EnvironmentAsync(builder, chatbot).ConfigureAwait(true);
            // A unique mailbox value identifies this builder's actual rendered realm even when other builders share
            // the test process, independently of Keycloak's container import annotation representation.
            string realmDirectory = Directory.EnumerateDirectories(Path.GetTempPath(), "hexalith-chatbot-keycloak-*")
                .Single(directory => File.Exists(Path.Combine(directory, "hexalith-realm.json"))
                    && File.ReadAllText(Path.Combine(directory, "hexalith-realm.json")).Contains(mailboxSecret, StringComparison.Ordinal));
            using JsonDocument realm = JsonDocument.Parse(await File.ReadAllTextAsync(
                Path.Combine(realmDirectory, "hexalith-realm.json"), TestContext.Current.CancellationToken).ConfigureAwait(true));
            foreach ((string owner, string audience, string operation) in new[]
            {
                ("Projects", "hexalith-eventstore", "projects:read"),
                ("Memories", "hexalith-memories-server", "memories:derived-store"),
            })
            {
                string prefix = "ChatBot__" + owner;
                EndpointReference endpoint = environment[prefix + "__Endpoint"].ShouldBeOfType<EndpointReference>();
                endpoint.Resource.ShouldBeSameAs(builder.Resources.Single(resource => resource.Name == owner.ToLowerInvariant()));
                endpoint.EndpointName.ShouldBe("http");
                string auth = prefix + "__OwnerAuthentication__";
                string clientId = "chatbot-" + owner.ToLowerInvariant() + "-owner";
                environment[auth + "ClientId"].ShouldBe(clientId);
                environment[auth + "TenantId"].ShouldBe("tenant-alpha");
                environment[auth + "Audience"].ShouldBe(audience);
                environment[auth + "Operation"].ShouldBe(operation);
                foreach (string setting in new[] { "Authority", "Issuer" })
                    environment[auth + setting].ShouldBeOfType<ReferenceExpression>().ValueExpression
                        .ShouldBe("{security.bindings.http.url}/realms/hexalith");
                environment[auth + "TokenEndpoint"].ShouldBeOfType<ReferenceExpression>().ValueExpression
                    .ShouldBe("{security.bindings.http.url}/realms/hexalith/protocol/openid-connect/token");
                ParameterResource secret = environment[auth + "ClientSecret"].ShouldBeOfType<ParameterResource>();
                secret.Secret.ShouldBeTrue();
                JsonElement client = realm.RootElement.GetProperty("clients").EnumerateArray()
                    .Single(value => value.GetProperty("clientId").GetString() == clientId);
                string.Equals(client.GetProperty("secret").GetString(),
                    await secret.GetValueAsync(TestContext.Current.CancellationToken).ConfigureAwait(true), StringComparison.Ordinal)
                    .ShouldBeTrue("The enrolled realm secret must equal the bound secret parameter.");
                client.GetProperty("fullScopeAllowed").GetBoolean().ShouldBeFalse();
                client.GetProperty("serviceAccountsEnabled").GetBoolean().ShouldBeTrue();
                client.GetProperty("defaultClientScopes").GetArrayLength().ShouldBe(0);
                string[] expectedScopes = ["platform-owner-audience." + audience, "platform-owner-operation." + operation.Replace(':', '.')];
                client.GetProperty("optionalClientScopes").EnumerateArray().Select(value => value.GetString()).ShouldBe(expectedScopes);
                JsonElement[] mappers = [.. client.GetProperty("protocolMappers").EnumerateArray()];
                AssertClaimMapper(mappers, "tenant_id", "tenant-alpha");
                AssertClaimMapper(mappers, "eventstore:permission", operation);
                mappers.Any(mapper => mapper.GetProperty("config").TryGetProperty("claim.name", out JsonElement name)
                    && name.GetString() == "eventstore:tenant").ShouldBeFalse();
                JsonElement subject = mappers.Single(mapper => mapper.GetProperty("name").GetString() == "owner-subject");
                subject.GetProperty("protocolMapper").GetString().ShouldBe("oidc-sub-mapper");
                subject.GetProperty("config").GetProperty("access.token.claim").GetString().ShouldBe("true");
                JsonElement[] scopes = [.. realm.RootElement.GetProperty("clientScopes").EnumerateArray()];
                JsonElement audienceMapper = scopes.Single(scope => scope.GetProperty("name").GetString() == expectedScopes[0])
                    .GetProperty("protocolMappers")[0];
                audienceMapper.GetProperty("config").GetProperty("included.custom.audience").GetString().ShouldBe(audience);
                audienceMapper.GetProperty("config").GetProperty("access.token.claim").GetString().ShouldBe("true");
                AssertClaimMapper([.. scopes.Single(scope => scope.GetProperty("name").GetString() == expectedScopes[1])
                    .GetProperty("protocolMappers").EnumerateArray()], "eventstore:operation", operation);
            }
            environment.ShouldNotContainKey("ChatBot__Projects__ApiToken");
            environment.ShouldNotContainKey("ChatBot__Memories__ApiToken");
            ProjectResource memories = builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == "memories");
            memories.Annotations.OfType<EndpointAnnotation>().Single(endpoint => endpoint.Name == "http").Port.ShouldBeNull();
            Dictionary<string, object> receiver = await EnvironmentAsync(builder, memories).ConfigureAwait(true);
            foreach (string setting in new[] { "Authority", "Issuer" })
                receiver["Authentication__JwtBearer__" + setting].ShouldBeOfType<ReferenceExpression>().ValueExpression
                    .ShouldBe("{security.bindings.http.url}/realms/hexalith");
            receiver["Authentication__JwtBearer__Audience"].ShouldBe("hexalith-memories-server");
            receiver["Authentication__JwtBearer__SigningKey"].ShouldBe("");
            receiver["Authentication__JwtBearer__RequireHttpsMetadata"].ShouldBe("false");
            receiver["Authentication__JwtBearer__ValidAlgorithms__0"].ShouldBe("RS256");
        }
        finally
        {
            await builder.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData("ChatBot:Projects:Endpoint", "http://localhost:65535")]
    [InlineData("ChatBot:Projects:ApiToken", "partial-project-token")]
    [InlineData("ChatBot:Memories:ApiToken", "partial-memory-token")]
    [InlineData("ChatBot:Projects:TenantId", "tenant-alpha")]
    [InlineData("ChatBot:Memories:Endpoint", "http://localhost:65535")]
    [InlineData("ChatBot:Memories:TenantId", "tenant-alpha")]
    [InlineData("ChatBot:Projects:OwnerAuthentication:ClientId", "partial-owner")]
    [InlineData("ChatBot:Memories:OwnerAuthentication:ClientSecret", "partial-secret")]
    public async Task PartialExplicitOverridesCannotFallBackToLocalCredentials(string key, string value)
    {
        Exception exception = await Should.ThrowAsync<Exception>(async () =>
        {
            IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
                .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(
                    ["--environment=Development", $"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={new string('a', 32)}", $"--{key}={value}"],
                    TestContext.Current.CancellationToken).ConfigureAwait(true);
            await builder.DisposeAsync().ConfigureAwait(true);
        });
        exception.ToString().ShouldContain("explicit external owner setup");
    }

    [Theory]
    [InlineData("Projects")]
    [InlineData("Memories")]
    public async Task CompleteStaticOverridesRejectMixedClientCredentials(string owner)
    {
        Exception exception = await Should.ThrowAsync<Exception>(async () =>
        {
            IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
                .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(
                    [.. StaticArgs, $"--ChatBot:{owner}:OwnerAuthentication:ClientId=mixed-owner"],
                    TestContext.Current.CancellationToken).ConfigureAwait(true);
            await builder.DisposeAsync().ConfigureAwait(true);
        });
        exception.ToString().ShouldContain("complete static tokens without mixed client credentials");
    }

    [Fact]
    public async Task CompleteStaticOverridesPreserveEndpointsTokensAndExplicitTenantBindings()
    {
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(
                [.. StaticArgs, "--ChatBot:Memories:Endpoint=https://external-memories.invalid/gateway",
                    "--ChatBot:Projects:TenantId=external-project-tenant", "--ChatBot:Memories:TenantId=external-memory-tenant"],
                TestContext.Current.CancellationToken).ConfigureAwait(true);
        try
        {
            Dictionary<string, object> environment = await EnvironmentAsync(builder,
                builder.Resources.Single(resource => resource.Name == "chatbot")).ConfigureAwait(true);
            environment["ChatBot__Projects__Endpoint"].ShouldBe("https://external-projects.invalid/gateway");
            environment["ChatBot__Memories__Endpoint"].ShouldBeOfType<ReferenceExpression>().ValueExpression
                .ShouldBe("https://external-memories.invalid/gateway");
            foreach ((string owner, string tenant, string token) in new[]
            {
                ("Projects", "external-project-tenant", "external-project-token"),
                ("Memories", "external-memory-tenant", "external-memory-token"),
            })
            {
                environment["ChatBot__" + owner + "__TenantId"].ShouldBe(tenant);
                ParameterResource parameter = environment["ChatBot__" + owner + "__ApiToken"].ShouldBeOfType<ParameterResource>();
                parameter.Secret.ShouldBeTrue();
                string.Equals(await parameter.GetValueAsync(TestContext.Current.CancellationToken).ConfigureAwait(true), token, StringComparison.Ordinal)
                    .ShouldBeTrue("The complete static override must retain its token.");
                environment.ShouldNotContainKey("ChatBot__" + owner + "__OwnerAuthentication__ClientId");
            }
        }
        finally
        {
            await builder.DisposeAsync().ConfigureAwait(true);
        }
    }

    private static readonly string[] StaticArgs =
    [
        "--environment=Development", $"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={new string('a', 32)}",
        "--ChatBot:Projects:Endpoint=https://external-projects.invalid/gateway",
        "--ChatBot:Projects:ApiToken=external-project-token", "--ChatBot:Memories:ApiToken=external-memory-token",
    ];

    private static async Task<Dictionary<string, object>> EnvironmentAsync(IDistributedApplicationTestingBuilder builder, IResource resource)
    {
        Dictionary<string, object> environment = new(StringComparer.Ordinal);
        EnvironmentCallbackContext context = new(builder.ExecutionContext, resource, environment, TestContext.Current.CancellationToken);
        foreach (EnvironmentCallbackAnnotation callback in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await callback.Callback(context).ConfigureAwait(true);
        return environment;
    }

    private static void AssertClaimMapper(JsonElement[] mappers, string name, string value)
    {
        JsonElement mapper = mappers.Single(mapper => mapper.GetProperty("config").TryGetProperty("claim.name", out JsonElement claim)
            && claim.GetString() == name);
        mapper.GetProperty("protocolMapper").GetString().ShouldBe("oidc-hardcoded-claim-mapper");
        mapper.GetProperty("config").GetProperty("claim.value").GetString().ShouldBe(value);
        mapper.GetProperty("config").GetProperty("access.token.claim").GetString().ShouldBe("true");
    }
}
