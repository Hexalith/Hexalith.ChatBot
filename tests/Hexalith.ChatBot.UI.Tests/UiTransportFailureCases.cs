using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Shared typed-client failures for every UI effect boundary.</summary>
public static class UiTransportFailureCases
{
    /// <summary>Gets body-less status, transient transport and programming-failure cases.</summary>
    public static TheoryData<string, int, string> Cases
    {
        get
        {
            TheoryData<string, int, string> cases = [];
            foreach ((int status, string code) in new[]
            {
                (401, "authentication_denied"), (403, "authorization_denied"), (404, "authorization_denied"),
                (400, "command_contract_invalid"), (422, "command_contract_invalid"), (409, "generic_denial"),
                (408, "dependency_degraded"), (429, "dependency_degraded"), (500, "dependency_degraded"), (503, "dependency_degraded"),
            })
            {
                foreach (string kind in new[] { "untyped", "null-body", "no-code" }) cases.Add(kind, status, code);
            }
            foreach (string kind in new[] { "http", "timeout", "task-timeout", "canceled-token-timeout" }) cases.Add(kind, 0, "dependency_degraded");
            cases.Add("programming", 0, "generic_denial");
            cases.Add("unknown-code", 503, "generic_denial");
            cases.Add("alias-timeout", 503, "dependency_degraded");
            cases.Add("alias-forbidden", 403, "authorization_denied");
            cases.Add("alias-conflict", 409, "invalid_lifecycle_transition");
            return cases;
        }
    }

    /// <summary>Creates the selected failure with restricted sentinel prose.</summary>
    /// <param name="kind">The failure shape.</param>
    /// <param name="status">The received HTTP status.</param>
    /// <returns>The exception supplied at the typed-client seam.</returns>
    public static Exception Create(string kind, int status)
        => kind switch
        {
            "http" => new HttpRequestException("restricted /private/secret"),
            "timeout" => new TimeoutException("restricted /private/secret"),
            "task-timeout" => new TaskCanceledException("restricted /private/secret", new TimeoutException("restricted")),
            "canceled-token-timeout" => new TaskCanceledException("restricted /private/secret", new TimeoutException("restricted"), new CancellationToken(canceled: true)),
            "programming" => new InvalidOperationException("restricted /private/secret"),
            "untyped" => new HexalithChatBotApiException("restricted", status, "restricted", null!, null),
            _ => new HexalithChatBotApiException<ProblemDetails>("restricted", status, "restricted", null!,
                kind == "null-body" ? null! : new ProblemDetails { Code = kind switch
                {
                    "unknown-code" => "restricted_private_unknown_code",
                    "alias-timeout" => "TIMEOUT",
                    "alias-forbidden" => "FORBIDDEN",
                    "alias-conflict" => "Conflict",
                    _ => null!,
                } }, null),
        };
    /// <summary>Creates explicit cancellation, including the real unset-token canceled-task shape.</summary>
    /// <param name="kind">The cancellation source.</param>
    /// <returns>The cancellation observed by the effect.</returns>
    public static OperationCanceledException Cancellation(string kind)
    {
        if (kind == "operation") return new OperationCanceledException();
        if (kind == "linked") return new TaskCanceledException("explicit cancellation", null, new CancellationToken(canceled: true));
        TaskCompletionSource<int> completion = new();
        completion.SetCanceled();
        try
        {
            completion.Task.GetAwaiter().GetResult();
        }
        catch (TaskCanceledException exception)
        {
            return exception;
        }
        throw new InvalidOperationException("The canceled task unexpectedly completed.");
    }

}
