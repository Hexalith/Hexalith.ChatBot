using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ProjectWorkspaceRouteContractTests
{
    private static readonly Regex RawTextareaTag = new(
        "<textarea(\\s|/|>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] RequiredWorkspaceKeys =
    [
        "ProjectWorkspace_Title",
        "ProjectWorkspace_Picker_Title",
        "ProjectWorkspace_Recents_Title",
        "ProjectWorkspace_State_ColdLoad",
        "ProjectWorkspace_State_NoProjectSelected",
        "ProjectWorkspace_State_EmptyProject",
        "ProjectWorkspace_State_ActiveConversation",
        "ProjectWorkspace_State_DependencyDegraded",
        "ProjectWorkspace_State_UnauthorizedRedacted",
        "ProjectWorkspace_State_ProjectSwitchSuccess",
        "ProjectWorkspace_ContextPanel_Title",
        "ProjectWorkspace_FilesPanel_Title",
    ];

    [Fact]
    public void ProjectWorkspaceShouldOwnRootRouteAndGovernedOperationsShouldUseExplicitRoute()
    {
        string workspace = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor");
        string governedOperations = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");
        string projectConversation = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor");

        workspace.ShouldContain("@page \"/\"");
        workspace.ShouldContain("data-chatbot-responsive-fixture=\"project-workspace\"");
        workspace.ShouldContain("id=\"project-workspace-title\"");
        workspace.ShouldContain("ChatBotProjectConversationWorkspace");
        // The landing route must not ship fabricated "authorized recents". They were untranslated English literals,
        // filtered by neither tenant nor authorization, with deep links that 403/404 -- the ungoverned fallback AC1
        // forbids. This assertion previously REQUIRED that fixture to be present.
        workspace.ShouldNotContain("ProjectWorkspaceAuthorizedRecentProject");
        workspace.ShouldNotContain("data-chatbot-authorized-recents-fixture");
        workspace.ShouldNotContain("Alpha project");
        workspace.ShouldContain("ProjectWorkspaceStateNoProjectSelected");
        workspace.ShouldNotContain("<FrontComposerShell", Case.Sensitive);
        workspace.ShouldNotContain("<FluentProviders", Case.Sensitive);
        workspace.ShouldNotContain("StoreInitializer", Case.Sensitive);
        workspace.ShouldNotContain("hero", Case.Insensitive);
        ShouldNotContainRawTextareaTag(workspace);

        governedOperations.ShouldContain("@page \"/governed-operations\"");
        governedOperations.ShouldNotContain("@page \"/\"");
        governedOperations.ShouldContain("<ChatBotConversationShell");
        governedOperations.ShouldContain("<ChatBotApprovalQueuePriorityView");

        projectConversation.ShouldContain("@page \"/projects/{ProjectId}/conversation\"");
        projectConversation.ShouldContain("ChatBotProjectConversationWorkspace");
    }

    [Fact]
    public void SelectedProjectWorkspaceShouldReuseS1ConversationContextAndAttachmentRendering()
    {
        string selectedProject = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor");

        selectedProject.ShouldContain("ChatBotConversationShell");
        selectedProject.ShouldContain("ChatBotProjectContextHeader");
        selectedProject.ShouldContain("ChatBotConversationStream");
        selectedProject.ShouldContain("ChatBotWhyProjectPanel");
        selectedProject.ShouldContain("ChatBotAttachmentConversationItem");
        selectedProject.ShouldContain("OpenProjectAssociationWhyPanelAction");
        selectedProject.ShouldContain("LoadProjectConversationAction(ProjectId)");
        selectedProject.ShouldContain("ProjectWorkspaceFilesPanelTitle");
        selectedProject.ShouldContain("ProjectWorkspaceContextPanelTitle");
        selectedProject.ShouldNotContain("<FrontComposerShell", Case.Sensitive);
        selectedProject.ShouldNotContain("providerPayload", Case.Sensitive);
        selectedProject.ShouldNotContain("RawAttachmentContent", Case.Sensitive);
    }

    [Fact]
    public void ProjectWorkspaceShouldDeclareExplicitLocalizedUxDr5States()
    {
        string workspace = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor");
        string selectedProject = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor");
        string keys = ReadProjectFile("src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs");
        string english = ReadProjectFile("src/Hexalith.ChatBot.UI/Localization/SharedResource.resx");
        string french = ReadProjectFile("src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx");

        foreach (string requiredKey in RequiredWorkspaceKeys)
        {
            keys.ShouldContain(requiredKey);
            english.ShouldContain($"name=\"{requiredKey}\"");
            french.ShouldContain($"name=\"{requiredKey}\"");
        }

        workspace.ShouldContain("ProjectWorkspaceStateNoProjectSelected");
        workspace.ShouldContain("ShowProjectSwitchSuccess");
        selectedProject.ShouldContain("ProjectWorkspaceStateColdLoad");
        selectedProject.ShouldContain("ProjectWorkspaceStateEmptyProject");
        selectedProject.ShouldContain("ProjectWorkspaceStateActiveConversation");
        selectedProject.ShouldContain("ProjectWorkspaceStateDependencyDegraded");
        selectedProject.ShouldContain("ProjectWorkspaceStateUnauthorizedRedacted");
        selectedProject.ShouldContain("ProjectWorkspaceStateProjectSwitchSuccess");
        selectedProject.ShouldNotContain("Exception", Case.Sensitive);
        selectedProject.ShouldNotContain("StackTrace", Case.Sensitive);
    }

    [Fact]
    public void ProjectWorkspaceStylesShouldUseSemanticTokensAndStableResponsiveDimensions()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain(".chatbot-project-workspace");
        css.ShouldContain(".chatbot-project-picker");
        css.ShouldContain(".chatbot-project-file-list");
        css.ShouldContain("grid-template-columns");
        css.ShouldContain("minmax");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldNotContain("#");
        css.ShouldNotContain("rgb(");
        css.ShouldNotContain("hsl(");
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(ProjectPath(relativePath));

    private static void ShouldNotContainRawTextareaTag(string content)
        => RawTextareaTag.Matches(content).ShouldBeEmpty("raw lowercase <textarea> tags are forbidden; FluentTextArea is allowed.");

    private static string ProjectPath(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return Path.Combine(directory.FullName, relativePath);
    }
}
