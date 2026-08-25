using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Mints the dedicated recovery-validator bearer from the topology Keycloak realm without retaining it.</summary>
internal static class RecoveryAccessTokenProvider
{
    public static async Task<string> AcquireAsync(DistributedApplication application, CancellationToken cancellationToken)
    {
        Dictionary<string, string> fields = new()
        {
            ["grant_type"] = "password",
            ["client_id"] = "hexalith-chatbot",
            ["username"] = "recovery-validator",
            ["password"] = "recovery-validator-pass",
            ["scope"] = "openid",
        };
        return await AcquireTokenAsync(application, fields, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<string> AcquireMailboxAsync(
        DistributedApplication application,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        Dictionary<string, string> fields = new()
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = RecoveryValidationTopology.MailboxClientId,
            ["client_secret"] = clientSecret,
            ["scope"] = "openid",
        };
        return await AcquireTokenAsync(application, fields, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Mints the realm's global-administrator bearer, used only to provision store-global EventStore state that a
    /// fresh deployment must activate before it can become ready.
    /// </summary>
    /// <param name="application">The composed topology.</param>
    /// <param name="cancellationToken">Cancels acquisition.</param>
    /// <returns>The global-administrator bearer.</returns>
    public static async Task<string> AcquireGlobalAdministratorAsync(
        DistributedApplication application,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> fields = new()
        {
            // The EventStore admin API validates tokens issued for its own client, not ChatBot's.
            ["grant_type"] = "password",
            ["client_id"] = "hexalith-eventstore",
            ["username"] = "admin-user",
            ["password"] = "admin-pass",
            ["scope"] = "openid",
        };
        return await AcquireTokenAsync(application, fields, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<string> AcquireControlAsync(
        DistributedApplication application,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> fields = new()
        {
            ["grant_type"] = "password",
            ["client_id"] = "hexalith-chatbot",
            ["username"] = "actor-beta",
            ["password"] = "actor-beta-pass",
            ["scope"] = "openid",
        };
        return await AcquireTokenAsync(application, fields, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> AcquireTokenAsync(
        DistributedApplication application,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        Uri security = application.GetEndpoint("security", "http");
        using FormUrlEncodedContent form = new(fields);
        string formBody = await form.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        Stopwatch timer = Stopwatch.StartNew();
        // Retained so the timeout below can name what Keycloak actually did for three minutes. Without it, an
        // identity provider that was never listening and one that answered 503 produced the identical message,
        // and the fail-safe coordinator then reduced both to the same causeless `unmeasurable` report.
        string lastAttemptOutcome = "no attempt completed";
        using HttpClient client = new() { BaseAddress = security, Timeout = TimeSpan.FromSeconds(15) };
        while (timer.Elapsed < TimeSpan.FromMinutes(3))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using StringContent content = new(formBody, Encoding.UTF8, "application/x-www-form-urlencoded");
                using HttpResponseMessage response = await client
                    .PostAsync("/realms/hexalith/protocol/openid-connect/token", content, cancellationToken)
                    .ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    try
                    {
                        using JsonDocument document = JsonDocument.Parse(
                            await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                        string? token = document.RootElement.GetProperty("access_token").GetString();
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            return token;
                        }
                    }
                    catch (JsonException)
                    {
                        // Keycloak returned 200 with a non-JSON or incomplete body; retry like transport failures.
                    }
                    catch (KeyNotFoundException)
                    {
                        // Missing access_token while the realm is still settling; retry within the bound.
                    }
                }
                else if (!IsRetryableStatus(response.StatusCode))
                {
                    throw new InvalidOperationException("Keycloak rejected a dedicated recovery identity.");
                }

                lastAttemptOutcome = $"status {(int)response.StatusCode}";
            }
            catch (HttpRequestException exception)
            {
                // Keycloak realm import is not ready yet.
                lastAttemptOutcome = $"transport failure ({exception.HttpRequestError})";
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Per-attempt listener timeout; retry within the overall bound.
                lastAttemptOutcome = "per-attempt 15s client timeout";
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Keycloak did not issue a dedicated recovery bearer before the deadline; last attempt: {lastAttemptOutcome}.");
    }

    internal static bool IsRetryableStatus(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
            (int)statusCode >= 500;
}
