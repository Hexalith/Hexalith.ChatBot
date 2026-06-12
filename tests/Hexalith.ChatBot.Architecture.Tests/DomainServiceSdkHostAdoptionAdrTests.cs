using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

public static class DomainServiceSdkHostAdoptionAdrTests
{
    [Fact]
    public static void DomainServiceSdkHostAdoptionAdr_ShouldRecordAcceptedDecisionAndSdkBindings()
    {
        string adr = ReadProjectFile("docs/adrs/domainservice-sdk-host-adoption.md");

        adr.ShouldContain("## Status");
        adr.ShouldContain("Accepted (2026-06-11, Story 11.1).");
        adr.ShouldContain("ChatBot adopts `Hexalith.EventStore.DomainService` as the target host layer.");
        adr.ShouldContain("explicitly rejected as the default direction");
        adr.ShouldContain("not recorded as a permanent exception");
        adr.ShouldContain("`AddEventStoreDomainService(...)` plus admission-chain registration plus");
        adr.ShouldContain("`UseEventStoreDomainService()`");
        adr.ShouldContain("`IDomainQueryHandler`");
        adr.ShouldContain("`IDomainProjectionHandler`");
        adr.ShouldContain("`IReadModelStore`");
        adr.ShouldContain("`ReadModelWritePolicy`");
        adr.ShouldContain("`IQueryCursorCodec`");
        adr.ShouldContain("`QueryCursorScope`");
        adr.ShouldContain("`AddEventStoreDomainTelemetry`");
        adr.ShouldContain("`AddEventStoreDomainStateStoreHealthCheck`");
        adr.ShouldContain("`AddEventStoreDomainModule(...)`");

        foreach (string endpoint in new[] { "POST /process", "POST /replay-state", "POST /query", "POST /project", "POST /admin/operational-index-metadata" })
        {
            adr.ShouldContain(endpoint);
        }
    }

    [Fact]
    public static void DomainServiceSdkHostAdoptionAdr_ShouldPreserveAdmissionAndMigrationOrder()
    {
        string adr = ReadProjectFile("docs/adrs/domainservice-sdk-host-adoption.md");

        adr.ShouldContain("pre-commit admission hook owned by Story 11.2");
        adr.ShouldContain("not a ChatBot-specific bypass");
        adr.ShouldContain("not a second command pipeline");
        adr.ShouldContain("not a weakening of the existing CommandGateway spine");
        adr.ShouldContain("preserve fail-closed admission");
        adr.ShouldContain("`11.2 -> 11.3/11.4 -> 11.5 -> 11.6`");
        adr.ShouldContain("Stories 11.2-11.6 must not start before this ADR is accepted.");
        adr.ShouldContain("Stories 11.5 and 11.6 land after Stories 8.7a and 8.7b");
        adr.ShouldContain("requires explicit submodule approval before any EventStore edit");
    }

    [Fact]
    public static void DomainServiceSdkHostAdoptionAdr_ShouldBoundExceptionsAndDecisionOnlyScope()
    {
        string adr = ReadProjectFile("docs/adrs/domainservice-sdk-host-adoption.md");

        adr.ShouldContain("## Exception Boundary");
        adr.ShouldContain("The only allowed retained hand-rolled exception is a thin local-development umbrella AppHost");
        adr.ShouldContain("Date: 2026-06-11.");
        adr.ShouldContain("Owner: ChatBot platform architecture owner.");
        adr.ShouldContain("local developer orchestration only");
        adr.ShouldContain("not a production domain-hosting pattern");
        adr.ShouldContain("Retirement/review trigger: Story 11.6");
        adr.ShouldContain("Story 11.1 is decision work only.");
        adr.ShouldContain("does not implement the platform hook");
        adr.ShouldContain("migrate endpoints or projections");
        adr.ShouldContain("reduce");
        adr.ShouldContain("remove `AppHost`/`Aspire`/`ServiceDefaults`");
    }

    [Fact]
    public static void ArchitectureD8_ShouldLinkAcceptedAdrAndMatchStoryElevenOneDecision()
    {
        string architecture = ReadProjectFile("_bmad-output/planning-artifacts/architecture.md");

        architecture.ShouldContain("D8");
        architecture.ShouldContain("[`docs/adrs/domainservice-sdk-host-adoption.md`](../../docs/adrs/domainservice-sdk-host-adoption.md)");
        architecture.ShouldContain("`Hexalith.EventStore.DomainService` SDK");
        architecture.ShouldContain("pre-commit admission hook");
        architecture.ShouldContain("Story 11.2");
        architecture.ShouldContain("`AddEventStoreDomainService()`");
        architecture.ShouldContain("admission-chain registration");
        architecture.ShouldContain("`UseEventStoreDomainService()`");
        architecture.ShouldContain("`IDomainQueryHandler`");
        architecture.ShouldContain("`IDomainProjectionHandler`");
        architecture.ShouldContain("`IReadModelStore` + `ReadModelWritePolicy`");
        architecture.ShouldContain("`IQueryCursorCodec`/`QueryCursorScope`");
        architecture.ShouldContain("`AddEventStoreDomainTelemetry`/`AddEventStoreDomainStateStoreHealthCheck`");
        architecture.ShouldContain("`AddEventStoreDomainModule(...)`");
        architecture.ShouldContain("gates Stories 11.2-11.6");
        architecture.ShouldContain("never a production domain-hosting bypass");
    }

    [Fact]
    public static void StoryElevenThree_ReadRoutes_ShouldUseSdkQueryDispatcherAndCursorCodec()
    {
        string program = ReadProjectFile("src/Hexalith.ChatBot.Server/Program.cs");
        string projectFile = ReadProjectFile("src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj");
        string handlers = ReadProjectFile("src/Hexalith.ChatBot.Server/Queries/ChatBotReadQueryHandlers.cs");

        projectFile.ShouldContain("Hexalith.EventStore.DomainService.csproj");
        program.ShouldContain("AddEventStoreDomainService");
        program.ShouldContain("AddEventStoreQueryCursorCodec(\"Hexalith.ChatBot.QueryCursor.v1\")");
        program.ShouldContain("DomainQueryDispatcher.ExecuteAsync");
        program.ShouldContain("\"/query\"");
        handlers.ShouldContain("IDomainQueryHandler");
        handlers.ShouldContain("IQueryCursorCodec");
        handlers.ShouldContain("QueryCursorScope.Create()");

        program.ShouldNotContain("ProjectConversationCursor");
        program.ShouldNotContain(".ReadPageAsync(");
        program.ShouldNotContain(".GetTaskIntentAsync(");
        program.ShouldNotContain(".EnumerateChain(");
    }

    [Fact]
    public static void StoryElevenThree_LocalProjectConversationCursorCodec_ShouldNotRegrow()
    {
        string[] cursorFiles = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.Server"), "*ProjectConversationCursor*.cs", SearchOption.AllDirectories)
            .Select(path => Path.GetFileName(path))
            .ToArray();

        cursorFiles.ShouldBe(["ProjectConversationCursorPosition.cs"], ignoreOrder: true);
        string source = ReadProjectFile("src/Hexalith.ChatBot.Server/Projections/ProjectConversationCursorPosition.cs");
        source.ShouldNotContain("HMACSHA256");
        source.ShouldNotContain("SigningKey");
        source.ShouldNotContain("Base64Url");
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath));

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
