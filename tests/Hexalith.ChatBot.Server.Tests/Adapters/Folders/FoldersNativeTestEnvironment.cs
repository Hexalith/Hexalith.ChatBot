using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
namespace Hexalith.ChatBot.Server.Tests.Adapters.Folders;
internal sealed class FoldersNativeTestEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "native-tests";
    public string ContentRootPath { get; set; } = ".";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
