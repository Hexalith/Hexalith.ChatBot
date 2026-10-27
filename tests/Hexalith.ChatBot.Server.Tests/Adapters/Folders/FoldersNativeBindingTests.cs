using Hexalith.ChatBot.Server.Adapters.Folders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
namespace Hexalith.ChatBot.Server.Tests.Adapters.Folders;
public sealed class FoldersNativeBindingTests
{
    [Fact]
    public void UnconfiguredBindingRetainsUnavailableStoreAndCompleteBindingOptsIn()
    {
        var services = new ServiceCollection(); services.AddSingleton<IFolderStore, UnavailableFolderStore>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        services.AddChatBotNativeFolders(configuration, new FoldersNativeTestEnvironment());
        using (var unconfigured = services.BuildServiceProvider()) unconfigured.GetRequiredService<IFolderStore>().ShouldBeOfType<UnavailableFolderStore>();
        configuration["ChatBot:Folders:Endpoint"] = "http://localhost:8080/";
        configuration["ChatBot:Folders:TenantId"] = "tenant-a"; configuration["ChatBot:Folders:ApiToken"] = "dedicated-test-token";
        services.AddChatBotNativeFolders(configuration, new FoldersNativeTestEnvironment());
        using var configured = services.BuildServiceProvider(); configured.GetRequiredService<IFolderStore>().ShouldBeOfType<FoldersFolderStore>();
    }
    [Theory]
    [InlineData("Endpoint")]
    [InlineData("TenantId")]
    [InlineData("ApiToken")]
    public void PartialBindingIsRejected(string missing)
    {
        var values = new Dictionary<string, string?> { ["ChatBot:Folders:Endpoint"] = "http://localhost/",
            ["ChatBot:Folders:TenantId"] = "tenant-a", ["ChatBot:Folders:ApiToken"] = "test-token" };
        values.Remove("ChatBot:Folders:" + missing);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddChatBotNativeFolders(configuration, new FoldersNativeTestEnvironment()));
    }
    [Theory]
    [InlineData("https://other.invalid/api/v1/folders/a/workspaces/b/native/task-lease")]
    [InlineData("https://owner.invalid/api/v1/folders/a/workspaces/b/files/add")]
    public async Task CredentialNeverLeavesTheConfiguredNativeBoundary(string destination)
    {
        bool sent = false;
        using var transport = new NativeAttachmentTestHandler((_, _) => { sent = true; return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)); });
        using var guard = new FoldersNativeBearerHandler(new Uri("https://owner.invalid/"), "tenant-a", "private-test-token") { InnerHandler = transport };
        using var http = new HttpClient(guard);
        await Should.ThrowAsync<InvalidOperationException>(() => http.GetAsync(destination, TestContext.Current.CancellationToken)).ConfigureAwait(true);
        sent.ShouldBeFalse();
    }
}
