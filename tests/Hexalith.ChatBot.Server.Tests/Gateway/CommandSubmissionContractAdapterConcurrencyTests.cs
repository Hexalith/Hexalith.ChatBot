using System.Reflection;
using System.Runtime.Loader;
using System.Text;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Gateway;

using Microsoft.AspNetCore.Http;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed class CommandSubmissionContractAdapterConcurrencyTests
{
    [Fact]
    public async Task ConcurrentFirstRequestsMustValidateAgainstColdNullabilityMetadata()
    {
        // A fresh load context gives this actual adapter a cold static context regardless
        // of which other endpoint tests have already validated these command types.
        AssemblyLoadContext isolated = new("cold-command-adapter-" + Guid.NewGuid(), isCollectible: true);
        isolated.Resolving += static (_, name) => AssemblyLoadContext.Default.Assemblies
            .FirstOrDefault(assembly => AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), name));
        try
        {
            Assembly assembly = isolated.LoadFromAssemblyPath(typeof(CommandSubmissionContractAdapter).Assembly.Location);
            MethodInfo read = assembly.GetType(typeof(CommandSubmissionContractAdapter).FullName!)!
                .GetMethod("ReadAsync", BindingFlags.Public | BindingFlags.Static)!;
            using Barrier firstUse = new(128);
            Task<(CommandSubmissionRequest? Request, string? Origin)>[] validations = Enumerable.Range(0, 128)
                .Select(_ => Task.Factory.StartNew(() =>
                {
                    DefaultHttpContext context = new();
                    context.Request.ContentType = "application/json";
                    context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""
                        {"commandId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","commandType":"RecordGovernedNote","command":{"noteId":"01ARZ3NDEKTSV4RRFFQ69G5FAW"},"requestSchemaVersion":"v1"}
                        """));
                    firstUse.SignalAndWait(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken).ShouldBeTrue();
                    return (Task<(CommandSubmissionRequest? Request, string? Origin)>)read.Invoke(null, [context, TestContext.Current.CancellationToken])!;
                }, TestContext.Current.CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap()).ToArray();
            var responses = await Task.WhenAll(validations).ConfigureAwait(true);
            responses.Length.ShouldBe(128);
            responses.All(response => response.Request?.CommandType == "RecordGovernedNote").ShouldBeTrue();
        }
        finally
        {
            isolated.Unload();
        }
    }
}
