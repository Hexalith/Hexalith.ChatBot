using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Story 1.3 R-B5 topology guard: the association subscription route refuses every delivery whose
/// <c>dapr-api-token</c> header does not match the app's <c>APP_API_TOKEN</c>. The local topology must give one
/// per-run token to both the chatbot app and the sidecar that presents it; otherwise association scoring, decisions
/// and corrections are refused and dead-lettered.
/// </summary>
public sealed class AssociationIngressAppChannelTokenTopologyTests
{
    private const string AppChannelTokenVariable = "APP_API_TOKEN";

    // CreateAsync must succeed, so supply the AppHost's fail-closed realm, Projects and Memories prerequisites.
    private static readonly string[] RenderedRealmArgs =
    [
        $"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={new string('a', 32)}",
        "--ChatBot:Projects:Endpoint=http://localhost:65535",
        $"--ChatBot:Projects:ApiToken={new string('b', 32)}",
        $"--ChatBot:Memories:ApiToken={new string('c', 32)}",
    ];

    [Fact]
    public async Task ChatBotAppAndItsOwnSidecarShareOneGeneratedAppChannelToken()
    {
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(RenderedRealmArgs, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        try
        {
            IResource chatBot = builder.Resources.Single(resource =>
                string.Equals(resource.Name, "chatbot", StringComparison.Ordinal));
            IResource sidecar = chatBot.Annotations.OfType<DaprSidecarAnnotation>().Single().Sidecar;

            string? appToken = await ResolveEnvironmentValueAsync(chatBot, AppChannelTokenVariable).ConfigureAwait(true);
            string? sidecarToken = await ResolveEnvironmentValueAsync(sidecar, AppChannelTokenVariable).ConfigureAwait(true);

            appToken.ShouldNotBeNullOrWhiteSpace();
            appToken.Length.ShouldBeGreaterThanOrEqualTo(64);
            sidecarToken.ShouldBe(appToken);
        }
        finally
        {
            await builder.DisposeAsync().ConfigureAwait(true);
        }
    }

    private static async Task<string?> ResolveEnvironmentValueAsync(IResource resource, string name)
    {
        Dictionary<string, object> environment = new(StringComparer.Ordinal);
        EnvironmentCallbackContext context = new(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            resource,
            environment,
            TestContext.Current.CancellationToken);
        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context).ConfigureAwait(true);
        }

        return environment.TryGetValue(name, out object? value)
            ? value switch
            {
                string text => text,
                IValueProvider provider => await provider.GetValueAsync(TestContext.Current.CancellationToken).ConfigureAwait(true),
                _ => value.ToString(),
            }
            : null;
    }
}
