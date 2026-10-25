using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Hexalith.ChatBot.Server.Tests.Adapters.OwnerServices;

internal sealed class OwnerAuthenticationTestEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "OwnerAuthenticationTests";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
