using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Serialization;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Test-only SDK wire transport for the actual owner tenant aggregate. State reconstruction uses SDK replay and
/// the owner's Apply methods; command outcomes come exclusively from the owner's existing Handle methods.
/// </summary>
internal sealed class MemoriesProbeDomainTransport
{
    private readonly Assembly _owner;
    private readonly Type _stateType;
    private readonly Type _aggregateType;
    private readonly MethodInfo _rehydrate;
    private int _replayedCommands;

    public MemoriesProbeDomainTransport(string serverAssemblyPath)
    {
        ServerAssemblyPath = Path.GetFullPath(serverAssemblyPath);
        File.Exists(ServerAssemblyPath).ShouldBeTrue();
        OwnerModulePath = Path.Combine(Path.GetDirectoryName(ServerAssemblyPath)!, "Hexalith.Memories.EventStore.dll");
        // Load only the owner module deployed alongside the exact live Aspire executable. No source fallback,
        // alternate build configuration, copied business rules, or compile-time sibling reference is allowed.
        OwnerModuleSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(OwnerModulePath))).ToLowerInvariant();
        _owner = Assembly.LoadFrom(OwnerModulePath);
        string.Equals(Path.GetFullPath(_owner.Location), OwnerModulePath,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal).ShouldBeTrue();
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_owner.Location))).ToLowerInvariant().ShouldBe(OwnerModuleSha256);
        OwnerModuleVersion = _owner.GetName().Version!.ToString();
        using JsonDocument dependencies = JsonDocument.Parse(File.ReadAllBytes(Path.ChangeExtension(ServerAssemblyPath, ".deps.json")));
        string ownerLibrary = dependencies.RootElement.GetProperty("libraries").EnumerateObject().Single(static library =>
            library.Name.StartsWith("Hexalith.Memories.EventStore/", StringComparison.Ordinal)).Name;
        // deps.json records the package/library version; AssemblyVersion has different semantics. Record both.
        OwnerLibraryVersion = ownerLibrary.Split('/')[1];
        dependencies.RootElement.GetProperty("targets").EnumerateObject().Any(target =>
            target.Value.TryGetProperty(ownerLibrary, out JsonElement library)
            && library.GetProperty("runtime").TryGetProperty("Hexalith.Memories.EventStore.dll", out _)).ShouldBeTrue();
        _stateType = OwnerType("States.MemoriesTenantAggregateState");
        _aggregateType = OwnerType("Aggregates.MemoriesTenantAggregate");
        _rehydrate = typeof(DomainStateReplay).GetMethod(nameof(DomainStateReplay.Rehydrate))!.MakeGenericMethod(_stateType);
    }

    public string OwnerModuleSha256 { get; }

    public string OwnerModulePath { get; }

    public string OwnerModuleVersion { get; }

    public string OwnerLibraryVersion { get; }

    public string ServerAssemblyPath { get; }

    public int ReplayedCommands => Volatile.Read(ref _replayedCommands);

    public DomainServiceWireResult Process(DomainServiceRequest request, string serviceSubject)
    {
        CommandEnvelope envelope = request.Command;
        if (envelope.TenantId is not ("tenant-alpha" or "tenant-beta") || envelope.Domain != "memories-tenants"
            || envelope.UserId != serviceSubject || envelope.AggregateId != envelope.TenantId)
        {
            throw new UnauthorizedAccessException("The owner transport accepts only the scoped authenticated fixture envelope.");
        }

        Type commandType = envelope.CommandType switch
        {
            "register-tenant" => OwnerType("Commands.RegisterTenantCommand"),
            "update-tenant-lifecycle-status" => OwnerType("Commands.UpdateTenantLifecycleStatusCommand"),
            _ => throw new UnauthorizedAccessException("The owner transport accepts only the two existing tenant lifecycle commands."),
        };
        object command = JsonSerializer.Deserialize(envelope.Payload, commandType, EventStorePayloadSerialization.Options)
            ?? throw new InvalidOperationException("The owner command payload is missing.");
        if (!string.Equals(commandType.GetProperty("TenantId")!.GetValue(command) as string, envelope.TenantId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("The owner payload tenant must match the authenticated envelope.");
        }

        object? state = _rehydrate.Invoke(null, [request.CurrentState]);
        if (state is not null)
        {
            Interlocked.Increment(ref _replayedCommands);
        }

        MethodInfo handle = _aggregateType.GetMethod("Handle", [commandType, _stateType])!;
        object result = handle.Invoke(null, [command, state]) ?? throw new InvalidOperationException("The owner returned no domain result.");
        IEnumerable events = (IEnumerable)result.GetType().GetProperty("Events")!.GetValue(result)!;
        List<DomainServiceWireEvent> wireEvents = [];
        foreach (object payload in events)
        {
            Type type = payload.GetType();
            wireEvents.Add(new DomainServiceWireEvent(type.FullName!, JsonSerializer.SerializeToUtf8Bytes(payload, type), "json"));
        }

        // MemoriesDomainResult supports success or idempotent no-op only. Preserve its exact event list; do not
        // synthesize acceptance, state, outcomes, or rejection events at this transport boundary.
        return new DomainServiceWireResult(false, wireEvents);
    }

    private Type OwnerType(string name)
        => _owner.GetType("Hexalith.Memories.EventStore.Domain." + name, throwOnError: true)!;
}
