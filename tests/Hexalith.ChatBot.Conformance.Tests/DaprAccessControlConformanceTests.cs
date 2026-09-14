using System.Text.RegularExpressions;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Conformance guards for the PRODUCTION deny-by-default DAPR access-control policy
/// (<c>src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml</c>).
/// </summary>
/// <remarks>
/// No deployment manifest in this repository consumes that file and the local Tier-3 topology loads the mTLS-off
/// <c>accesscontrol.local.yaml</c> instead, so these tests are the only thing that can catch a drift between the
/// policy and the route surface the EventStore DomainService SDK actually maps onto the chatbot app.
/// </remarks>
public static partial class DaprAccessControlConformanceTests
{
    /// <summary>
    /// Every appId the production policy is allowed to name. An appId outside this set means a policy was added
    /// without review, so the exhaustiveness assertion below fails rather than silently ignoring it.
    /// </summary>
    private static readonly string[] KnownPolicyAppIds = ["eventstore", "chatbot"];

    /// <summary>
    /// The POST routes the EventStore SDK maps onto the chatbot domain service. Held here as the reviewed,
    /// intentional grant; <see cref="EventStorePolicyMustGrantEveryMappedSdkPostRoute"/> proves this list is still
    /// the SDK's own mapped literal set, so an SDK route addition cannot pass unnoticed.
    /// </summary>
    private static readonly string[] ExpectedEventStoreOperations =
    [
        "/process",
        "/replay-state",
        "/query",
        "/project",
        "/project/v2",
        "/project/v2/reconcile",
        "/project/rebuild/v1",
        "/project/rebuild/stage/v1",
        "/project/rebuild/commit/v1",
        "/project/rebuild/abort/v1",
        "/project/rebuild/verify/v1",
        "/project/rebuild/shared/v1",
        "/admin/operational-index-metadata",
    ];

    [Fact]
    public static void ChatBotAccessControlMustBeDenyByDefaultAndNotCopyFoldersAllowPolicy()
    {
        YamlMappingNode accessControl = LoadAccessControl();
        Scalar(accessControl, "defaultAction").ShouldBe("deny");

        YamlMappingNode[] policies = [.. Sequence(accessControl, "policies").Children.Cast<YamlMappingNode>()];

        // Exhaustive, not "the two named ones": a third policy carrying defaultAction: allow previously passed
        // because the assertions only ever looked up eventstore and chatbot by name.
        string[] appIds = [.. policies.Select(policy => Scalar(policy, "appId") ?? "<missing>")];
        appIds.ShouldBe(KnownPolicyAppIds, ignoreOrder: true);

        foreach (YamlMappingNode policy in policies)
        {
            string? appId = Scalar(policy, "appId");
            Scalar(policy, "defaultAction").ShouldBe(
                "deny",
                $"Policy '{appId}' must be deny-by-default; the production posture allows no default-allow policy.");
            Scalar(policy, "trustDomain").ShouldBe("public", $"Policy '{appId}' must pin the SPIFFE trust domain.");
        }

        YamlMappingNode chatBot = policies.Single(policy => Scalar(policy, "appId") == "chatbot");
        Sequence(chatBot, "operations").Children.ShouldBeEmpty();
    }

    [Fact]
    public static void EventStorePolicyMustGrantExactlyTheInvokedPostRouteSet()
    {
        YamlMappingNode accessControl = LoadAccessControl();
        YamlMappingNode eventStore = Sequence(accessControl, "policies").Children
            .Cast<YamlMappingNode>()
            .Single(policy => Scalar(policy, "appId") == "eventstore");

        YamlMappingNode[] operations = [.. Sequence(eventStore, "operations").Children.Cast<YamlMappingNode>()];
        string[] names = [.. operations.Select(operation => Scalar(operation, "name") ?? "<missing>")];

        // Exact set equality in both directions: a missing route silently denies a live cross-app call, and an
        // extra route widens the production grant beyond what was reviewed.
        names.ShouldBe(ExpectedEventStoreOperations, ignoreOrder: true);
        names.Distinct(StringComparer.Ordinal).Count().ShouldBe(names.Length, "Operation names must not repeat.");

        foreach (YamlMappingNode operation in operations)
        {
            string? name = Scalar(operation, "name");
            Scalar(operation, "action").ShouldBe("allow", $"Operation '{name}' must be an explicit allow.");
            string[] verbs = [.. Sequence(operation, "httpVerb").Children
                .Cast<YamlScalarNode>()
                .Select(static verb => verb.Value ?? "<missing>")];
            verbs.ShouldBe(["POST"], customMessage: $"Operation '{name}' must be POST-scoped.");
        }
    }

    /// <summary>
    /// Drift guard: parses the POST route literals the EventStore DomainService SDK maps and fails naming any route
    /// the production ACL does not grant. Without this, adding a route to the SDK silently produces a denied
    /// cross-app call in the only environment (mTLS-on production) where the policy is enforced.
    /// </summary>
    [Fact]
    public static void EventStorePolicyMustGrantEveryMappedSdkPostRoute()
    {
        string[] mappedRoutes = ReadMappedSdkPostRoutes();

        // A parser that silently matched nothing would turn this guard vacuous. The SDK has mapped at least the
        // command/replay/query trio since Story 1.1; a collapse below that is a broken parser, not a shrunken SDK.
        mappedRoutes.Length.ShouldBeGreaterThanOrEqualTo(
            3,
            "The SDK route parser matched almost nothing — EventStoreDomainServiceExtensions.cs changed shape and "
            + "this drift guard must be re-pointed rather than left passing vacuously.");
        mappedRoutes.ShouldContain("/process");

        YamlMappingNode accessControl = LoadAccessControl();
        YamlMappingNode eventStore = Sequence(accessControl, "policies").Children
            .Cast<YamlMappingNode>()
            .Single(policy => Scalar(policy, "appId") == "eventstore");
        HashSet<string> granted = [.. Sequence(eventStore, "operations").Children
            .Cast<YamlMappingNode>()
            .Select(operation => Scalar(operation, "name"))
            .OfType<string>()];

        string[] missing = [.. mappedRoutes.Where(route => !granted.Contains(route)).Order(StringComparer.Ordinal)];
        missing.ShouldBeEmpty(
            "The EventStore DomainService SDK maps POST routes the production ACL does not grant, so those calls "
            + "are denied under mTLS. Add them to accesscontrol.yaml (and to ExpectedEventStoreOperations): "
            + string.Join(", ", missing));

        string[] ungrantable = [.. granted.Where(route => !mappedRoutes.Contains(route, StringComparer.Ordinal)).Order(StringComparer.Ordinal)];
        ungrantable.ShouldBeEmpty(
            "The production ACL grants routes the SDK no longer maps, widening the grant past the real surface: "
            + string.Join(", ", ungrantable));
    }

    /// <summary>
    /// Reads the literal POST routes mapped by the read-only EventStore DomainService SDK. Three shapes carry a
    /// route literal: a direct <c>MapPost("/x", …)</c>, the <c>MapNamedProjectionRebuildEndpoint(app, "/x", …)</c>
    /// helper call, and the shared-rebuild helper's <c>const string route = "/x"</c>. The two helper bodies map
    /// through a <c>route</c> variable, so they are never double-counted.
    /// </summary>
    private static string[] ReadMappedSdkPostRoutes()
    {
        string sdkPath = Path.Combine(
            RepositoryRoot(),
            "references",
            "Hexalith.EventStore",
            "src",
            "Hexalith.EventStore.DomainService",
            "EventStoreDomainServiceExtensions.cs");
        File.Exists(sdkPath).ShouldBeTrue(
            $"The EventStore SDK source must be present to check ACL route drift. Missing: {sdkPath}. "
            + "Initialize the root-declared references submodules with `git submodule update --init`.");

        string source = File.ReadAllText(sdkPath);
        return [.. MappedPostRouteRegex()
            .Matches(source)
            .Select(match => match.Groups["route"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];
    }

    [GeneratedRegex(
        "(?:MapPost\\(\\s*|MapNamedProjectionRebuildEndpoint\\(\\s*app\\s*,\\s*|const\\s+string\\s+route\\s*=\\s*)\"(?<route>/[^\"]*)\"",
        RegexOptions.CultureInvariant)]
    private static partial Regex MappedPostRouteRegex();

    private static YamlMappingNode LoadAccessControl()
    {
        string policyPath = Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "DaprComponents",
            "accesscontrol.yaml");
        using StreamReader reader = File.OpenText(policyPath);
        YamlStream yaml = new();
        yaml.Load(reader);

        YamlMappingNode root = (YamlMappingNode)yaml.Documents.Single().RootNode;
        return Mapping(Mapping(root, "spec"), "accessControl");
    }

    private static YamlMappingNode Mapping(YamlMappingNode parent, string key)
        => (YamlMappingNode)parent.Children[new YamlScalarNode(key)];

    private static YamlSequenceNode Sequence(YamlMappingNode parent, string key)
        => (YamlSequenceNode)parent.Children[new YamlScalarNode(key)];

    private static string? Scalar(YamlMappingNode parent, string key)
        => ((YamlScalarNode)parent.Children[new YamlScalarNode(key)]).Value;

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
