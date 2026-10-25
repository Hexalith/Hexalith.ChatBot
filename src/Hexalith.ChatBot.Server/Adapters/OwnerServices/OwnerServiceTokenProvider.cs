using System.Text.Json;

using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Hexalith.ChatBot.Server.Adapters.OwnerServices;

/// <summary>Owns an isolated SDK issuer and its framework options monitors for one owner identity.</summary>
internal sealed class OwnerServiceTokenProvider : IDisposable
{
    private JwtWorkloadAssertionIssuer _issuer;
    private readonly OptionsMonitor<JwtBearerAuthenticationOptions> _contract;
    private readonly OptionsMonitor<WorkloadAssertionIssuerOptions> _credentials;
    private readonly OwnerServiceAuthenticationOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly IHttpClientFactory _clients;
    private readonly WorkloadSecurityClock _clock;
    private readonly ILogger<JwtWorkloadAssertionIssuer> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    /// <summary>Creates a separate SDK issuer without sharing workload credentials or options.</summary>
    public OwnerServiceTokenProvider(OwnerServiceAuthenticationOptions options, IHostEnvironment environment,
        IHttpClientFactory clients, WorkloadSecurityClock clock, ILogger<JwtWorkloadAssertionIssuer> logger)
    {
        _options = options;
        _environment = environment;
        _clients = clients;
        _clock = clock;
        _logger = logger;
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Contract:Authority"] = options.Authority,
            ["Contract:Issuer"] = options.Issuer,
            ["Contract:Audience"] = options.Audience,
            ["Contract:RequireHttpsMetadata"] = (new Uri(options.Authority!).Scheme == Uri.UriSchemeHttps).ToString(),
            ["Contract:AllowedAlgorithms:0"] = "RS256",
            ["Credentials:ClientId"] = options.ClientId,
            ["Credentials:ClientSecret"] = options.ClientSecret,
            ["Credentials:TokenEndpoint"] = options.TokenEndpoint,
            ["Credentials:AudienceScopePrefix"] = "platform-owner-audience.",
            ["Credentials:OperationScopePrefix"] = "platform-owner-operation.",
        }).Build();
        _contract = Monitor<JwtBearerAuthenticationOptions>(configuration.GetSection("Contract"), EventStoreWorkloadAuthenticationDefaults.JwtContractOptionsName);
        _credentials = Monitor<WorkloadAssertionIssuerOptions>(configuration.GetSection("Credentials"), Options.DefaultName);
        _issuer = CreateIssuer();
    }

    /// <summary>Acquires the exact owner token through the SDK and validates its REST identity and tenant claims.</summary>
    public async ValueTask<string?> AcquireAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            try
            {
                string? token = await _issuer.IssueAsync(new(_options.Audience, _options.Operation), cancellationToken).ConfigureAwait(false);
                if (token is null) return null;
                var parsed = new JsonWebToken(token);
                if (IsUsable(parsed)) return token;
            }
            catch (Exception exception) when (exception is ArgumentException or SecurityTokenException or JsonException or OverflowException)
            {
                // In particular, the SDK's lifetime conversion can reject an out-of-range numeric date.
                // Neither invalid responses nor configured secrets become diagnostics or owner requests.
            }
            // REST checks follow the SDK cache admission. Reset only this isolated SDK issuer so a corrected
            // authority response can recover immediately; all acquisition/cache/refresh behavior remains SDK-owned.
            _issuer.Dispose();
            _issuer = CreateIssuer();
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool IsUsable(JsonWebToken parsed)
    {
        using JsonDocument payload = JsonDocument.Parse(Base64UrlEncoder.DecodeBytes(parsed.EncodedPayload));
        JsonElement root = payload.RootElement;
        long now = _clock.GetUtcNow().ToUnixTimeSeconds();
        const long maximumUnixSeconds = 253402300799;
        if (!root.TryGetProperty("exp", out JsonElement expiry) || expiry.ValueKind != JsonValueKind.Number
            || !expiry.TryGetInt64(out long expiresAt) || expiresAt <= 0 || expiresAt <= now || expiresAt > maximumUnixSeconds)
            return false;
        if (root.TryGetProperty("nbf", out JsonElement date)
            && (date.ValueKind != JsonValueKind.Number || !date.TryGetInt64(out long value) || value < 0 || value > now))
            return false;
        // Projects shares EventStore's REST audience, so owner tokens must never carry the eventstore:tenant claim that
        // EventStore's tenant-scoped endpoints accept on its own; both receivers derive the tenant from tenant_id.
        return !string.IsNullOrWhiteSpace(parsed.Subject) && parsed.Issuer == _options.Issuer
            && parsed.Alg == "RS256"
            && ExactClaim("azp", _options.ClientId!)
            && ExactClaim("tenant_id", _options.TenantId!)
            && !parsed.Claims.Any(claim => claim.Type == "eventstore:tenant")
            && OwnerServiceAuthenticationOptions.ReadSingleTenant(parsed) == _options.TenantId
            && ExactClaim("eventstore:permission", _options.Operation);

        bool ExactClaim(string name, string value)
        {
            string[] values = [.. parsed.Claims.Where(claim => claim.Type == name).Select(claim => claim.Value).Distinct(StringComparer.Ordinal)];
            return values.Length == 1 && values[0] == value;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _gate.Wait();
        try
        {
            if (_disposed) return;
            _disposed = true;
            _issuer.Dispose();
            _contract.Dispose();
            _credentials.Dispose();
        }
        finally
        {
            // Pending acquisition waiters must still enter and observe the disposed state safely.
            _gate.Release();
        }
    }

    private JwtWorkloadAssertionIssuer CreateIssuer() => new(_contract, _credentials, _environment, _clients, _clock, _logger);

    private static OptionsMonitor<T> Monitor<T>(IConfiguration configuration, string name) where T : class, new()
        => new(new OptionsFactory<T>([new NamedConfigureFromConfigurationOptions<T>(name, configuration)], []), [], new OptionsCache<T>());
}
