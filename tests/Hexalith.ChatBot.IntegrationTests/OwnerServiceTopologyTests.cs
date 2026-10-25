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
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(
                ["--environment=Development", $"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={new string('a', 32)}"],
                TestContext.Current.CancellationToken).ConfigureAwait(true);
        try
        {
            ProjectResource chatbot = builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == "chatbot");
            var environment = new Dictionary<string, object>();
            EnvironmentCallbackContext context = new(builder.ExecutionContext, chatbot, environment, TestContext.Current.CancellationToken);
            foreach (EnvironmentCallbackAnnotation callback in chatbot.Annotations.OfType<EnvironmentCallbackAnnotation>())
                await callback.Callback(context).ConfigureAwait(true);
            environment["ChatBot__Projects__Endpoint"].ShouldBeOfType<EndpointReference>();
            environment["ChatBot__Memories__Endpoint"].ShouldBeOfType<EndpointReference>();
            environment["ChatBot__Projects__OwnerAuthentication__ClientId"].ShouldBe("chatbot-projects-owner");
            environment["ChatBot__Memories__OwnerAuthentication__ClientId"].ShouldBe("chatbot-memories-owner");
            environment["ChatBot__Projects__OwnerAuthentication__TenantId"].ShouldBe("tenant-alpha");
            environment["ChatBot__Projects__OwnerAuthentication__ClientSecret"].ShouldBeOfType<ParameterResource>().Secret.ShouldBeTrue();
            environment["ChatBot__Memories__OwnerAuthentication__ClientSecret"].ShouldBeOfType<ParameterResource>().Secret.ShouldBeTrue();
            environment.ShouldNotContainKey("ChatBot__Projects__ApiToken");
            environment.ShouldNotContainKey("ChatBot__Memories__ApiToken");
            ProjectResource memories = builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == "memories");
            memories.Annotations.OfType<EndpointAnnotation>().Single(endpoint => endpoint.Name == "http").Port.ShouldBeNull();
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
}
