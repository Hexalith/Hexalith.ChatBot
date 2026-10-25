using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Projections.DerivedStores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Adapters.OwnerServices;

/// <summary>Sends the DI-resolved live adapters through real factory handlers to a signed-token receiver.</summary>
public sealed class OwnerServiceLiveAdapterTests
{
    [Fact]
    public async Task CredentialAdaptersAuthenticateAndPreserveNonRootOwnerDirectories()
    {
        using RSA signingKey = RSA.Create(2048);
        RsaSecurityKey key = new(signingKey);
        SigningCredentials signing = new(key, SecurityAlgorithms.RsaSha256);
        string? issuerIdentifier = null;
        List<string> authorizedPaths = [];
        string? persistedEntry = null;
        OwnerAuthenticationTestServer issuer = await OwnerAuthenticationTestServer.StartAsync(async context =>
        {
            context.Request.Path.Value.ShouldBe("/realms/local/protocol/openid-connect/token");
            IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
            bool projects = form["client_id"] == "projects-owner";
            (form["client_secret"] == (projects ? "projects-secret" : "memories-secret")).ShouldBeTrue();
            string audience = projects ? "hexalith-eventstore" : "hexalith-memories-server";
            string operation = projects ? "projects:read" : "memories:derived-store";
            form["scope"].ToString().ShouldBe("platform-owner-audience." + audience + " platform-owner-operation." + operation.Replace(':', '.'));
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = issuerIdentifier, Audience = audience, SigningCredentials = signing,
                IssuedAt = now.UtcDateTime, NotBefore = now.AddSeconds(-1).UtcDateTime, Expires = now.AddMinutes(2).UtcDateTime,
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = "synthetic-owner-subject", ["azp"] = projects ? "projects-owner" : "memories-owner",
                    ["tenant_id"] = "tenant-alpha", ["eventstore:tenant"] = "tenant-alpha",
                    ["eventstore:permission"] = operation, ["eventstore:operation"] = operation,
                },
            });
            await context.Response.WriteAsJsonAsync(new { access_token = token, expires_in = 120 }, context.RequestAborted).ConfigureAwait(false);
        });
        try
        {
            // Preserve the exact OIDC identifier, including its trailing slash.
            issuerIdentifier = new Uri(issuer.Endpoint, "realms/local/").AbsoluteUri;
            OwnerAuthenticationTestServer receiver = await OwnerAuthenticationTestServer.StartAsync(async context =>
            {
                bool projects = context.Request.Path.StartsWithSegments("/projects-gateway");
                string audience = projects ? "hexalith-eventstore" : "hexalith-memories-server";
                if (context.User.Identity?.IsAuthenticated != true || !context.User.HasClaim("aud", audience)
                    || !context.User.HasClaim("tenant_id", "tenant-alpha"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
                authorizedPaths.Add(context.Request.Path);
                if (projects)
                {
                    context.Request.Path.Value.ShouldBe("/projects-gateway/api/v1/projects/project-1/context");
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("""{"projectId":"project-1","assemblyOutcome":"assembled","lifecycle":"active","freshness":"fresh","memoryReferences":[{"referenceId":"case-1","referenceKind":"memory","referenceState":"included"}]}""", context.RequestAborted).ConfigureAwait(false);
                }
                else if (HttpMethods.IsPut(context.Request.Method))
                {
                    using StreamReader reader = new(context.Request.Body);
                    persistedEntry = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                }
                else
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(persistedEntry!, context.RequestAborted).ConfigureAwait(false);
                }
            }, services => services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true, ValidIssuer = issuerIdentifier, ValidateAudience = true,
                    ValidAudiences = ["hexalith-eventstore", "hexalith-memories-server"],
                    ValidateIssuerSigningKey = true, IssuerSigningKey = key,
                    ValidateLifetime = true, RequireExpirationTime = true, ClockSkew = TimeSpan.Zero,
                };
            }));
            try
            {
                Dictionary<string, string?> settings = new()
                {
                    ["ChatBot:Memories:UseLiveBacking"] = "true", ["ChatBot:UseDaprWorkflowRuntime"] = "true",
                };
                foreach ((string owner, string audience, string operation) in new[]
                {
                    ("Projects", "hexalith-eventstore", "projects:read"),
                    ("Memories", "hexalith-memories-server", "memories:derived-store"),
                })
                {
                    string prefix = "ChatBot:" + owner;
                    string lower = owner.ToLowerInvariant();
                    settings[prefix + ":Endpoint"] = new Uri(receiver.Endpoint, lower + "-gateway").AbsoluteUri;
                    settings[prefix + ":OwnerAuthentication:Authority"] = issuerIdentifier;
                    settings[prefix + ":OwnerAuthentication:Issuer"] = issuerIdentifier;
                    settings[prefix + ":OwnerAuthentication:TokenEndpoint"] = new Uri(issuer.Endpoint, "realms/local/protocol/openid-connect/token").AbsoluteUri;
                    settings[prefix + ":OwnerAuthentication:ClientId"] = lower + "-owner";
                    settings[prefix + ":OwnerAuthentication:ClientSecret"] = lower + "-secret";
                    settings[prefix + ":OwnerAuthentication:TenantId"] = "tenant-alpha";
                    settings[prefix + ":OwnerAuthentication:Audience"] = audience;
                    settings[prefix + ":OwnerAuthentication:Operation"] = operation;
                }
                ServiceCollection services = new();
                services.AddChatBotMemoriesDerivedStores(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
                    new OwnerAuthenticationTestEnvironment());
                using ServiceProvider provider = services.BuildServiceProvider();
                (await provider.GetRequiredService<IMemoriesCaseResolver>().ResolveCaseIdAsync(
                    "tenant-alpha", "project-1", "correlation-1", TestContext.Current.CancellationToken)).ShouldBe("case-1");
                IDerivedStore store = provider.GetRequiredService<IDerivedStore>();
                await store.PutAsync(DerivedStoreClass.VectorIndex, "tenant-alpha", "entry-1",
                    DerivedStoreEntry.Create("entry-1", "digest-1"), TestContext.Current.CancellationToken);
                DerivedStoreEntry? result = await store.GetAsync(DerivedStoreClass.VectorIndex, "tenant-alpha", "entry-1", TestContext.Current.CancellationToken);
                result.ShouldBe(DerivedStoreEntry.Create("entry-1", "digest-1"));
                using JsonDocument persisted = JsonDocument.Parse(persistedEntry!);
                persisted.RootElement.GetProperty("contentDigest").GetString().ShouldBe("digest-1");
                authorizedPaths.Count.ShouldBe(3);
                authorizedPaths.Skip(1).ShouldAllBe(path => path.StartsWith("/memories-gateway/api/v1/tenants/tenant-alpha/", StringComparison.Ordinal));
            }
            finally
            {
                await receiver.DisposeAsync();
            }
        }
        finally
        {
            await issuer.DisposeAsync();
        }
    }
}
