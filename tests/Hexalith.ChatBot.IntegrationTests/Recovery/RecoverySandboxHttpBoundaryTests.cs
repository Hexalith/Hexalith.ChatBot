using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using Hexalith.ChatBot.RecoverySandbox;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Exercises the actual sandbox HTTP handler without Aspire, Dapr or a downstream ChatBot service.</summary>
public sealed class RecoverySandboxHttpBoundaryTests
{
    [Fact]
    public async Task InvalidLaneHeadersAreRejectedBeforeAnyWorkerProcessing()
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        ProcessStartInfo start = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(typeof(RecoveryNotificationIdentity).Assembly.Location);
        foreach (string argument in new[]
        {
            "--urls=http://127.0.0.1:0",
            "--Recovery:Enabled=true",
            $"--Recovery:TenantRef={RecoveryValidationTopology.LogicalTenantRef}",
            $"--Recovery:StorageTenantRef={RecoveryValidationTopology.StorageTenantRef}",
            "--Recovery:ControllerSecret=boundary-test-controller",
            "--Recovery:ProviderMessageId=boundary-test-message",
            "--Recovery:ChatBotBaseAddress=http://127.0.0.1:1",
            "--Logging:Console:FormatterName=simple",
            "--Logging:LogLevel:Default=Information",
        })
        {
            start.ArgumentList.Add(argument);
        }

        TaskCompletionSource<Uri> listening = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Process process = new() { StartInfo = start, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, args) =>
        {
            const string marker = "Now listening on: ";
            if (args.Data is { } line && line.IndexOf(marker, StringComparison.Ordinal) is int index && index >= 0
                && Uri.TryCreate(line[(index + marker.Length)..].Trim(), UriKind.Absolute, out Uri? address))
            {
                listening.TrySetResult(address);
            }
        };
        process.Exited += (_, _) => listening.TrySetException(new InvalidOperationException("Sandbox exited before publishing its listener."));
        process.Start().ShouldBeTrue();
        Task<string> stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);
        process.BeginOutputReadLine();
        try
        {
            using HttpClient client = new() { BaseAddress = await listening.Task.WaitAsync(deadline.Token) };
            client.DefaultRequestHeaders.Add("X-Recovery-Controller-Secret", "boundary-test-controller");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "boundary-test-bearer");
            string route = $"/recovery/{Uri.EscapeDataString(RecoveryValidationTopology.LogicalTenantRef)}/m365-subscription-failure";
            using HttpResponseMessage before = await client.GetAsync($"{route}/status", deadline.Token);
            before.StatusCode.ShouldBe(HttpStatusCode.OK);
            foreach ((string[] lanes, string phase) in new (string[], string)[]
            {
                ([], "recovery"),
                (["unknown"], "recovery"),
                (["graph", "graph"], "recovery"),
                (["graph"], "checkpoint"),
            })
            {
                using HttpRequestMessage request = new(HttpMethod.Post, $"{route}/process");
                if (lanes.Length > 0)
                {
                    request.Headers.Add(RecoveryNotificationIdentity.ScenarioLaneHeaderName, lanes);
                }

                request.Headers.Add(RecoveryNotificationIdentity.HeaderName, phase);
                using HttpResponseMessage refusal = await client.SendAsync(request, deadline.Token);
                refusal.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            }

            using HttpResponseMessage after = await client.GetAsync($"{route}/status", deadline.Token);
            after.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument snapshot = JsonDocument.Parse(await after.Content.ReadAsStringAsync(deadline.Token));
            snapshot.RootElement.GetProperty("processingAttempts").GetInt32().ShouldBe(0);
            snapshot.RootElement.GetProperty("submitted").GetInt32().ShouldBe(0);
            snapshot.RootElement.GetProperty("recoverableFailures").GetInt32().ShouldBe(0);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            using CancellationTokenSource cleanup = new(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(cleanup.Token);
            _ = await stderr.WaitAsync(cleanup.Token);
        }
    }
}
