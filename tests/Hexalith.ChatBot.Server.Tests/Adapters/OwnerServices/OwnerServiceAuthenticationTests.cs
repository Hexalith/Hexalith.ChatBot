using System.Net;
using System.Text;
using System.Text.Json;
using Hexalith.ChatBot.Server.Adapters.OwnerServices;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Adapters.OwnerServices;

/// <summary>Synthetic issuer responses exercise the real SDK acquisition/cache path; live signing is verified with Keycloak.</summary>
public sealed class OwnerServiceAuthenticationTests
{
    [Fact]
    public async Task ConcurrentCallsReuseTheSdkTokenAndRefreshBeforeExpiry()
    {
        OwnerAuthenticationTestClock time = new();
        OwnerServiceAuthenticationOptions options = Load();
        int acquisitions = 0;
        int sends = 0;
        string? firstToken = null;
        using OwnerAuthenticationTestTransport issuer = new(async (request, cancellationToken) =>
        {
            request.RequestUri.ShouldBe(new Uri("https://issuer.invalid/realms/local/protocol/openid-connect/token"));
            string form = await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            form.ShouldContain("grant_type=client_credentials");
            form.ShouldContain("client_id=chatbot-projects-owner");
            form.ShouldContain("scope=platform-owner-audience.hexalith-eventstore+platform-owner-operation.projects.read");
            Interlocked.Increment(ref acquisitions);
            string token = Token(time.GetUtcNow(), options);
            firstToken ??= token;
            return Response(token);
        });
        using OwnerServiceTokenProvider tokens = new(options, new OwnerAuthenticationTestEnvironment(), issuer,
            new(time), NullLogger<JwtWorkloadAssertionIssuer>.Instance);
        using OwnerAuthenticationTestTransport owner = new((request, _) =>
        {
            request.Headers.Authorization!.Scheme.ShouldBe("Bearer");
            request.Headers.Authorization.Parameter.ShouldNotBeNullOrWhiteSpace();
            Interlocked.Increment(ref sends);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using HttpMessageInvoker client = new(new OwnerServiceBearerHandler(options, tokens) { InnerHandler = owner });
        await Task.WhenAll(Enumerable.Range(0, 12).Select(async _ =>
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "https://projects.invalid/api/projects/p/context");
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(false);
            request.Headers.Authorization!.Parameter.ShouldBe(firstToken);
        }));
        acquisitions.ShouldBe(1);
        sends.ShouldBe(12);
        time.Advance(TimeSpan.FromSeconds(95));
        using HttpRequestMessage refreshed = new(HttpMethod.Get, "https://projects.invalid/api/projects/p/context");
        using HttpResponseMessage refreshedResponse = await client.SendAsync(refreshed, TestContext.Current.CancellationToken);
        acquisitions.ShouldBe(2);
        refreshed.Headers.Authorization!.Parameter.ShouldNotBe(firstToken);
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("malformed")]
    [InlineData("expired")]
    [InlineData("audience")]
    [InlineData("operation")]
    [InlineData("tenant")]
    [InlineData("eventstore-tenant")]
    [InlineData("identity")]
    [InlineData("issuer")]
    [InlineData("algorithm")]
    [InlineData("missing-subject")]
    [InlineData("empty-subject")]
    [InlineData("permission")]
    [InlineData("missing-permission")]
    [InlineData("oversized")]
    [InlineData("missing-exp")]
    [InlineData("zero-exp")]
    [InlineData("negative-exp")]
    [InlineData("out-of-range-exp")]
    [InlineData("future-nbf")]
    [InlineData("out-of-range-nbf")]
    public async Task FailedOrMisScopedAcquisitionSendsNoOwnerRequest(string failure)
    {
        OwnerServiceAuthenticationOptions options = Load();
        using OwnerAuthenticationTestTransport issuer = new((_, _) => Task.FromResult(failure switch
        {
            "failed" => new HttpResponseMessage(HttpStatusCode.Unauthorized),
            "malformed" => Response("not-a-token"),
            "oversized" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string('x', 65537)) },
            _ => Response(Token(DateTimeOffset.UtcNow, options, failure)),
        }));
        using OwnerServiceTokenProvider tokens = new(options, new OwnerAuthenticationTestEnvironment(), issuer,
            WorkloadSecurityClock.System, NullLogger<JwtWorkloadAssertionIssuer>.Instance);
        int sends = 0;
        using OwnerAuthenticationTestTransport owner = new((_, _) =>
        {
            sends++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using HttpMessageInvoker client = new(new OwnerServiceBearerHandler(options, tokens) { InnerHandler = owner });
        using HttpRequestMessage request = new(HttpMethod.Get, "https://projects.invalid/api/projects/p/context");
        InvalidOperationException denied = await Should.ThrowAsync<InvalidOperationException>(() => client.SendAsync(request, TestContext.Current.CancellationToken));
        denied.Message.ShouldBe("owner_token_unavailable");
        denied.ToString().ShouldNotContain(options.ClientSecret!);
        sends.ShouldBe(0);
    }

    [Theory]
    [InlineData("issuer", "iss")]
    [InlineData("algorithm", "alg")]
    [InlineData("missing-subject", "sub")]
    [InlineData("permission", "eventstore:permission")]
    [InlineData("eventstore-tenant", "eventstore:tenant")]
    [InlineData("out-of-range-exp", "ArgumentOutOfRangeException")]
    public async Task RejectionDiagnosticsNameOnlyTheOwnerAndFailedCheck(string failure, string check)
    {
        OwnerServiceAuthenticationOptions options = Load();
        string token = Token(DateTimeOffset.UtcNow, options, failure);
        using OwnerAuthenticationTestTransport issuer = new((_, _) => Task.FromResult(Response(token)));
        OwnerAuthenticationTestLogger logger = new();
        using OwnerServiceTokenProvider tokens = new(options, new OwnerAuthenticationTestEnvironment(), issuer,
            WorkloadSecurityClock.System, logger, "Projects");
        (await tokens.AcquireAsync(TestContext.Current.CancellationToken)).ShouldBeNull();
        string message = logger.Messages.Single(value => value.StartsWith("Owner Projects", StringComparison.Ordinal));
        message.ShouldContain(check);
        message.ShouldNotContain(options.ClientSecret!);
        message.ShouldNotContain(token);
        message.ShouldNotContain("foreign");
        message.ShouldNotContain(options.TenantId!);
    }

    [Theory]
    [InlineData("eventstore:current-tenant", false)]
    [InlineData("tenantId", false)]
    [InlineData("tenants", false)]
    [InlineData("memories:tenant", true)]
    [InlineData("tenants", true)]
    [InlineData("tenant", true)]
    [InlineData("tid", true)]
    public async Task ConflictingReceiverTenantSelectorsNeverReachAnOwner(string selector, bool memories)
    {
        OwnerServiceAuthenticationOptions options = memories ? new()
        {
            Endpoint = new("https://memories.invalid/"), Authority = "https://issuer.invalid/realms/local",
            Issuer = "https://issuer.invalid/realms/local", TokenEndpoint = "https://issuer.invalid/realms/local/token",
            ClientId = "memories-owner", ClientSecret = "memory-secret", TenantId = "tenant-alpha",
            Audience = "hexalith-memories-server", Operation = "memories:derived-store",
        } : Load();
        using OwnerAuthenticationTestTransport issuer = new((_, _) => Task.FromResult(Response(
            Token(DateTimeOffset.UtcNow, options, mutate: claims => claims[selector] = new[] { "tenant-alpha", "tenant-beta" }))));
        using OwnerServiceTokenProvider tokens = new(options, new OwnerAuthenticationTestEnvironment(), issuer,
            WorkloadSecurityClock.System, NullLogger<JwtWorkloadAssertionIssuer>.Instance);
        int ownerRequests = 0;
        using OwnerAuthenticationTestTransport owner = new((_, _) =>
        {
            ownerRequests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using HttpMessageInvoker client = new(new OwnerServiceBearerHandler(options, tokens) { InnerHandler = owner });
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri(options.Endpoint, "api/probe"));
        (await Should.ThrowAsync<InvalidOperationException>(() => client.SendAsync(request, TestContext.Current.CancellationToken)))
            .Message.ShouldBe("owner_token_unavailable");
        ownerRequests.ShouldBe(0);
    }

    [Fact]
    public async Task ACorrectedIssuerResponseRecoversImmediatelyAfterRestValidationRejectsTheCachedToken()
    {
        OwnerServiceAuthenticationOptions options = Load();
        int acquisitions = 0;
        using OwnerAuthenticationTestTransport issuer = new((_, _) => Task.FromResult(Response(
            Token(DateTimeOffset.UtcNow, options, Interlocked.Increment(ref acquisitions) == 1 ? "tenant" : null))));
        using OwnerServiceTokenProvider tokens = new(options, new OwnerAuthenticationTestEnvironment(), issuer,
            WorkloadSecurityClock.System, NullLogger<JwtWorkloadAssertionIssuer>.Instance);
        (await tokens.AcquireAsync(TestContext.Current.CancellationToken)).ShouldBeNull();
        string? corrected = await tokens.AcquireAsync(TestContext.Current.CancellationToken);
        corrected.ShouldNotBeNull();
        (await tokens.AcquireAsync(TestContext.Current.CancellationToken)).ShouldBe(corrected);
        acquisitions.ShouldBe(2);
    }

    [Fact]
    public async Task DisposalWaitsForAnAcquisitionAndPreventsSubsequentUse()
    {
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        OwnerServiceAuthenticationOptions options = Load();
        using OwnerAuthenticationTestTransport issuer = new(async (_, _) =>
        {
            started.SetResult();
            await release.Task.ConfigureAwait(false);
            return Response(Token(DateTimeOffset.UtcNow, options));
        });
        using OwnerServiceTokenProvider tokens = new(options, new OwnerAuthenticationTestEnvironment(), issuer,
            WorkloadSecurityClock.System, NullLogger<JwtWorkloadAssertionIssuer>.Instance);
        Task<string?> acquisition = tokens.AcquireAsync(TestContext.Current.CancellationToken).AsTask();
        await started.Task.ConfigureAwait(true);
        Task disposing = Task.Run(tokens.Dispose, TestContext.Current.CancellationToken);
        release.SetResult();
        (await acquisition.ConfigureAwait(true)).ShouldNotBeNull();
        await disposing.ConfigureAwait(true);
        await Should.ThrowAsync<ObjectDisposedException>(() => tokens.AcquireAsync(TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task TrailingIssuerIdentifierIsPreservedDuringAcquisition()
    {
        OwnerServiceAuthenticationOptions options = Load("Issuer", "https://issuer.invalid/realms/local/");
        options.Issuer.ShouldBe("https://issuer.invalid/realms/local/");
        options.TokenEndpoint.ShouldBe("https://issuer.invalid/realms/local/protocol/openid-connect/token");
        using OwnerAuthenticationTestTransport issuer = new((_, _) => Task.FromResult(Response(Token(DateTimeOffset.UtcNow, options))));
        using OwnerServiceTokenProvider tokens = new(options, new OwnerAuthenticationTestEnvironment(), issuer,
            WorkloadSecurityClock.System, NullLogger<JwtWorkloadAssertionIssuer>.Instance);
        (await tokens.AcquireAsync(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("https://foreign.invalid/api/projects/p/context")]
    [InlineData("http://projects.invalid/api/projects/p/context")]
    [InlineData("https://projects.invalid:8443/api/projects/p/context")]
    [InlineData("https://secret@projects.invalid/api/projects/p/context")]
    public async Task ForeignOrInsecureOwnerDestinationsAreRejectedBeforeAcquisition(string destination)
    {
        using OwnerAuthenticationTestTransport issuer = new((_, _) => throw new InvalidOperationException("Issuer must not be contacted."));
        OwnerServiceAuthenticationOptions options = Load();
        using OwnerServiceTokenProvider tokens = new(options, new OwnerAuthenticationTestEnvironment(), issuer,
            WorkloadSecurityClock.System, NullLogger<JwtWorkloadAssertionIssuer>.Instance);
        using HttpMessageInvoker client = new(new OwnerServiceBearerHandler(options, tokens));
        using HttpRequestMessage request = new(HttpMethod.Get, destination);
        (await Should.ThrowAsync<InvalidOperationException>(() => client.SendAsync(request, TestContext.Current.CancellationToken)))
            .Message.ShouldBe("owner_request_destination_denied");
    }

    [Theory]
    [InlineData("TokenEndpoint", "https://foreign.invalid/token")]
    [InlineData("TokenEndpoint", "https://issuer.invalid/another-realm/token")]
    [InlineData("TokenEndpoint", "http://remote.invalid/token")]
    [InlineData("ClientSecret", "")]
    [InlineData("TenantId", "")]
    [InlineData("Audience", "other")]
    [InlineData("Operation", "projects:write")]
    [InlineData("SigningKey", "secret")]
    public void PartialMixedOrForeignCredentialConfigurationFailsClosed(string field, string value)
    {
        Should.Throw<InvalidOperationException>(() => Load(field, value));
    }

    [Fact]
    public void StaticTokenSetupIsRetainedAndCannotMixWithClientCredentials()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ChatBot:Projects:Endpoint"] = "https://projects.invalid",
            ["ChatBot:Projects:ApiToken"] = "operator-provided-token",
        }).Build();
        OwnerServiceAuthenticationOptions.Load(configuration, "Projects", "hexalith-eventstore", "projects:read", new OwnerAuthenticationTestEnvironment())
            .ApiToken.ShouldBe("operator-provided-token");
        Should.Throw<InvalidOperationException>(() => Load("../ApiToken", "mixed"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("tenant-beta")]
    public async Task AmbiguousStaticJwtTenantsCannotBeReboundOrSentToAnOwner(string? explicitTenant)
    {
        int ownerRequests = 0;
        using OwnerAuthenticationTestTransport owner = new((_, _) =>
        {
            ownerRequests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ChatBot:Projects:Endpoint"] = "https://projects.invalid",
            ["ChatBot:Projects:ApiToken"] = Token(DateTimeOffset.UtcNow, Load(), "multi-tenant"),
            ["ChatBot:Projects:TenantId"] = explicitTenant,
        }).Build();
        (await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            OwnerServiceAuthenticationOptions options = OwnerServiceAuthenticationOptions.Load(configuration,
                "Projects", "hexalith-eventstore", "projects:read", new OwnerAuthenticationTestEnvironment());
            using HttpMessageInvoker client = new(new OwnerServiceBearerHandler(options, null) { InnerHandler = owner });
            using HttpRequestMessage request = new(HttpMethod.Get, "https://projects.invalid/api/projects/p/context");
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        })).Message.ShouldContain("exactly one unambiguous tenant");
        ownerRequests.ShouldBe(0);
    }

    [Theory]
    [InlineData("eventstore:current-tenant")]
    [InlineData("tenantId")]
    [InlineData("tid")]
    [InlineData("tenants")]
    public async Task ProjectsStaticJwtRejectsEveryConflictingReceiverTenantSelector(string selector)
    {
        int ownerRequests = 0;
        using OwnerAuthenticationTestTransport owner = new((_, _) =>
        {
            ownerRequests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ChatBot:Projects:Endpoint"] = "https://projects.invalid",
            ["ChatBot:Projects:ApiToken"] = Token(DateTimeOffset.UtcNow, Load(), mutate: claims => claims[selector] = "tenant-beta"),
            ["ChatBot:Projects:TenantId"] = "tenant-alpha",
        }).Build();
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            OwnerServiceAuthenticationOptions options = OwnerServiceAuthenticationOptions.Load(configuration,
                "Projects", "hexalith-eventstore", "projects:read", new OwnerAuthenticationTestEnvironment());
            using HttpMessageInvoker client = new(new OwnerServiceBearerHandler(options, null) { InnerHandler = owner });
            using HttpRequestMessage request = new(HttpMethod.Get, new Uri(options.Endpoint, "api/probe"));
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        });
        ownerRequests.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("tenant-alpha")]
    public void AStaticJwtWithOneConsistentTenantRetainsItsBinding(string? explicitTenant)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ChatBot:Projects:Endpoint"] = "https://projects.invalid",
            ["ChatBot:Projects:ApiToken"] = Token(DateTimeOffset.UtcNow, Load()),
            ["ChatBot:Projects:TenantId"] = explicitTenant,
        }).Build();
        OwnerServiceAuthenticationOptions.Load(configuration, "Projects", "hexalith-eventstore", "projects:read", new OwnerAuthenticationTestEnvironment())
            .TenantId.ShouldBe("tenant-alpha");
    }

    [Fact]
    public async Task AStaticJwtWithOneTenantCannotBeReboundToAForeignExplicitTenant()
    {
        int ownerRequests = 0;
        using OwnerAuthenticationTestTransport owner = new((_, _) =>
        {
            ownerRequests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        string token = Token(DateTimeOffset.UtcNow, Load());
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ChatBot:Projects:Endpoint"] = "https://projects.invalid",
            ["ChatBot:Projects:ApiToken"] = token,
            ["ChatBot:Projects:TenantId"] = "tenant-beta",
        }).Build();
        InvalidOperationException denied = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            OwnerServiceAuthenticationOptions options = OwnerServiceAuthenticationOptions.Load(configuration,
                "Projects", "hexalith-eventstore", "projects:read", new OwnerAuthenticationTestEnvironment());
            using HttpMessageInvoker client = new(new OwnerServiceBearerHandler(options, null) { InnerHandler = owner });
            using HttpRequestMessage request = new(HttpMethod.Get, new Uri(options.Endpoint, "api/probe"));
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        });
        denied.Message.ShouldBe("ChatBot:Projects:TenantId must match the static token's single tenant.");
        denied.ToString().ShouldNotContain(token);
        ownerRequests.ShouldBe(0);
    }

    [Theory]
    [InlineData("[\"tenant-alpha\"]")]
    [InlineData(" [ \"tenant-alpha\" ] ")]
    public void ProjectsStaticJwtNormalizesASingleEncodedTenant(string encodedTenants)
    {
        string token = Token(DateTimeOffset.UtcNow, Load(), mutate: claims =>
        {
            claims.Remove("tenant_id");
            claims["tenants"] = encodedTenants;
        });
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ChatBot:Projects:Endpoint"] = "https://projects.invalid",
            ["ChatBot:Projects:ApiToken"] = token,
        }).Build();
        OwnerServiceAuthenticationOptions.Load(configuration, "Projects", "hexalith-eventstore", "projects:read", new OwnerAuthenticationTestEnvironment())
            .TenantId.ShouldBe("tenant-alpha");
    }

    [Theory]
    [InlineData("[\"tenant-alpha\",\"tenant-beta\"]")]
    [InlineData("[\"tenant-beta\"]")]
    [InlineData("[\"tenant-alpha\"")]
    [InlineData("[1]")]
    [InlineData("[null]")]
    [InlineData("[\"tenant-alpha\",null]")]
    [InlineData("[]")]
    [InlineData("[\"\"]")]
    [InlineData("[\" \"]")]
    [InlineData("{\"tenant-alpha\":true}")]
    public async Task ProjectsStaticJwtRejectsInvalidOrConflictingEncodedTenantListsBeforeOwnerAccess(string encodedTenants)
    {
        int ownerRequests = 0;
        using OwnerAuthenticationTestTransport owner = new((_, _) =>
        {
            ownerRequests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        string token = Token(DateTimeOffset.UtcNow, Load(), mutate: claims => claims["tenants"] = encodedTenants);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ChatBot:Projects:Endpoint"] = "https://projects.invalid",
            ["ChatBot:Projects:ApiToken"] = token,
            ["ChatBot:Projects:TenantId"] = "tenant-alpha",
        }).Build();
        InvalidOperationException denied = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            OwnerServiceAuthenticationOptions options = OwnerServiceAuthenticationOptions.Load(configuration,
                "Projects", "hexalith-eventstore", "projects:read", new OwnerAuthenticationTestEnvironment());
            using HttpMessageInvoker client = new(new OwnerServiceBearerHandler(options, null) { InnerHandler = owner });
            using HttpRequestMessage request = new(HttpMethod.Get, new Uri(options.Endpoint, "api/probe"));
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        });
        denied.Message.ShouldBe("ChatBot:Projects:ApiToken must carry exactly one unambiguous tenant in a static JWT.");
        denied.ToString().ShouldNotContain(token);
        ownerRequests.ShouldBe(0);
    }

    [Fact]
    public void ExternalMemoriesStaticJwtRetainsReceiverEnforcedMultipleTenantClaims()
    {
        OwnerServiceAuthenticationOptions memoryToken = new()
        {
            Endpoint = new("https://memories.invalid"), Audience = "hexalith-memories-server", Operation = "memories:derived-store",
            TenantId = "tenant-alpha", ClientId = "memories-owner", Issuer = "https://issuer.invalid/realms/local",
        };
        string token = Token(DateTimeOffset.UtcNow, memoryToken, "multi-memory-tenant");
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ChatBot:Memories:Endpoint"] = "https://memories.invalid",
            ["ChatBot:Memories:ApiToken"] = token,
        }).Build();
        OwnerServiceAuthenticationOptions.Load(configuration, "Memories", "hexalith-memories-server", "memories:derived-store", new OwnerAuthenticationTestEnvironment())
            .ApiToken.ShouldBe(token);
    }

    [Fact]
    public async Task CancellationIsPreservedWithoutAnOwnerRequest()
    {
        using OwnerAuthenticationTestTransport issuer = new(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        OwnerServiceAuthenticationOptions options = Load();
        using OwnerServiceTokenProvider tokens = new(options, new OwnerAuthenticationTestEnvironment(), issuer,
            WorkloadSecurityClock.System, NullLogger<JwtWorkloadAssertionIssuer>.Instance);
        using CancellationTokenSource cancellation = new();
        Task<string?> acquisition = tokens.AcquireAsync(cancellation.Token).AsTask();
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => acquisition);
    }

    private static OwnerServiceAuthenticationOptions Load(string? field = null, string? value = null)
    {
        Dictionary<string, string?> settings = new()
        {
            ["ChatBot:Projects:Endpoint"] = "https://projects.invalid",
            ["ChatBot:Projects:OwnerAuthentication:Authority"] = "https://issuer.invalid/realms/local",
            ["ChatBot:Projects:OwnerAuthentication:Issuer"] = "https://issuer.invalid/realms/local",
            ["ChatBot:Projects:OwnerAuthentication:TokenEndpoint"] = "https://issuer.invalid/realms/local/protocol/openid-connect/token",
            ["ChatBot:Projects:OwnerAuthentication:ClientId"] = "chatbot-projects-owner",
            ["ChatBot:Projects:OwnerAuthentication:ClientSecret"] = "dedicated-owner-secret",
            ["ChatBot:Projects:OwnerAuthentication:TenantId"] = "tenant-alpha",
            ["ChatBot:Projects:OwnerAuthentication:Audience"] = "hexalith-eventstore",
            ["ChatBot:Projects:OwnerAuthentication:Operation"] = "projects:read",
        };
        if (field is not null) settings[field == "../ApiToken" ? "ChatBot:Projects:ApiToken" : "ChatBot:Projects:OwnerAuthentication:" + field] = value;
        return OwnerServiceAuthenticationOptions.Load(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
            "Projects", "hexalith-eventstore", "projects:read", new OwnerAuthenticationTestEnvironment());
    }

    private static HttpResponseMessage Response(string token) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(new { access_token = token, expires_in = 120 })),
    };

    private static string Token(DateTimeOffset now, OwnerServiceAuthenticationOptions options, string? failure = null,
        Action<Dictionary<string, object?>>? mutate = null)
    {
        Dictionary<string, object?> claims = new()
        {
            ["iss"] = options.Issuer,
            ["sub"] = "dedicated-owner-service-account",
            ["azp"] = failure == "identity" ? "workload" : options.ClientId,
            ["aud"] = failure == "audience" ? "foreign" : options.Audience,
            ["eventstore:operation"] = failure == "operation" ? "projects:write" : options.Operation,
            ["eventstore:permission"] = options.Operation,
            ["tenant_id"] = failure == "multi-memory-tenant" ? new[] { "tenant-alpha", "tenant-beta" }
                : (object?)(failure == "tenant" ? "tenant-beta" : options.TenantId),
            ["exp"] = now.AddSeconds(failure == "expired" ? -1 : 120).ToUnixTimeSeconds(),
        };
        switch (failure)
        {
            case "issuer": claims["iss"] = "https://foreign.invalid"; break;
            case "missing-subject": claims.Remove("sub"); break;
            case "empty-subject": claims["sub"] = ""; break;
            case "permission": claims["eventstore:permission"] = "projects:write"; break;
            case "missing-permission": claims.Remove("eventstore:permission"); break;
            case "multi-tenant": claims["eventstore:tenant"] = new[] { "tenant-alpha", "tenant-beta" }; break;
            case "eventstore-tenant": claims["eventstore:tenant"] = options.TenantId; break;
            case "missing-exp": claims.Remove("exp"); break;
            case "zero-exp": claims["exp"] = 0; break;
            case "negative-exp": claims["exp"] = -1; break;
            case "out-of-range-exp": claims["exp"] = long.MaxValue; break;
            case "future-nbf": claims["nbf"] = now.AddSeconds(60).ToUnixTimeSeconds(); break;
            case "out-of-range-nbf": claims["nbf"] = long.MaxValue; break;
        }
        mutate?.Invoke(claims);
        string payload = JsonSerializer.Serialize(claims);
        static string Encode(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        string algorithm = failure == "algorithm" ? "HS256" : "RS256";
        return Encode(JsonSerializer.Serialize(new { alg = algorithm, typ = "JWT" })) + "." + Encode(payload) + ".synthetic-signature";
    }
}
