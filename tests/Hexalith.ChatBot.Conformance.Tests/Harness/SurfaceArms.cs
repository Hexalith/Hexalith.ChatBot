using System.Text;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

/// <summary>
/// The one surface-agnostic semantic intent every arm submits: record a governed note with this id. Equivalent
/// input across surfaces (FR86) means each arm's own raw representation parses back to the identical typed
/// <see cref="RecordGovernedNote"/> command record.
/// </summary>
/// <param name="NoteId">The governed-note aggregate ULID the intent targets.</param>
internal sealed record SemanticIntent(string NoteId);

/// <summary>
/// A swappable surface driver. The ONLY per-surface variation is the declared
/// <see cref="ChatBotSurfaceOrigin"/> and how this surface parses its own raw input into the one typed command;
/// the arm constructs only an <see cref="IChatBotCommand"/> and never replicates a gateway stage. Epic 5 Story
/// 5.4 can substitute the real <c>.Cli</c>/<c>.Mcp</c> adapters behind this same interface without touching the
/// assertion engine.
/// </summary>
internal interface ISurfaceArm
{
    /// <summary>The surface name (for labels/diagnostics).</summary>
    string Name { get; }

    /// <summary>The origin this surface declares at the boundary — the single permitted cross-arm delta.</summary>
    ChatBotSurfaceOrigin Origin { get; }

    /// <summary>Parses this surface's own raw input shape into the one typed command.</summary>
    /// <param name="intent">The surface-agnostic semantic intent.</param>
    /// <returns>The typed command, identical across arms for equivalent input.</returns>
    RecordGovernedNote ParseCommand(SemanticIntent intent);
}

/// <summary>UI arm: a form field carries the note id (mirrors GovernedOperationService submitting origin Ui).</summary>
internal sealed class UiSurfaceArm : ISurfaceArm
{
    public string Name => "ui";

    public ChatBotSurfaceOrigin Origin => ChatBotSurfaceOrigin.Ui;

    public RecordGovernedNote ParseCommand(SemanticIntent intent)
    {
        // UI binds a single form value straight to the typed command.
        string formNoteId = intent.NoteId;
        return new RecordGovernedNote(formNoteId);
    }
}

/// <summary>CLI arm: an argv vector is parsed (`record-governed-note --note-id &lt;id&gt;`) into the command.</summary>
internal sealed class CliSurfaceArm : ISurfaceArm
{
    public string Name => "cli";

    public ChatBotSurfaceOrigin Origin => ChatBotSurfaceOrigin.Cli;

    public RecordGovernedNote ParseCommand(SemanticIntent intent)
    {
        // A thin CLI shim: the same intent expressed as command-line arguments, parsed back to the typed command.
        string[] argv = ["record-governed-note", "--note-id", intent.NoteId];
        int flag = Array.IndexOf(argv, "--note-id");
        if (flag < 0 || flag + 1 >= argv.Length)
        {
            throw new FormatException("CLI arm: missing --note-id argument.");
        }

        return new RecordGovernedNote(argv[flag + 1]);
    }
}

/// <summary>MCP arm: a tool-call argument map is parsed into the command.</summary>
internal sealed class McpSurfaceArm : ISurfaceArm
{
    public string Name => "mcp";

    public ChatBotSurfaceOrigin Origin => ChatBotSurfaceOrigin.Mcp;

    public RecordGovernedNote ParseCommand(SemanticIntent intent)
    {
        // A thin MCP shim: the same intent expressed as a tool-call argument map, parsed back to the command.
        Dictionary<string, string> toolArguments = new(StringComparer.Ordinal)
        {
            ["tool"] = "record_governed_note",
            ["noteId"] = intent.NoteId,
        };
        if (!toolArguments.TryGetValue("noteId", out string? noteId) || string.IsNullOrWhiteSpace(noteId))
        {
            throw new FormatException("MCP arm: missing noteId tool argument.");
        }

        return new RecordGovernedNote(noteId.Normalize(NormalizationForm.FormC));
    }
}

/// <summary>The three M0 surface arms exercised by the differential-conformance harness.</summary>
internal static class SurfaceArms
{
    public static IReadOnlyList<ISurfaceArm> All { get; } = [new UiSurfaceArm(), new CliSurfaceArm(), new McpSurfaceArm()];
}
