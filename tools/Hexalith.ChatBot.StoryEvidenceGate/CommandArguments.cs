namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Parses the gate's dependency-free command-line grammar.
/// </summary>
public sealed class CommandArguments
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedOptions =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["validate"] = Set(
                "repository-root", "policy", "story", "contract", "target-status", "base", "head", "results", "report"),
            ["attest"] = Set("repository-root", "policy", "contract", "base", "head", "results"),
            ["detect"] = Set("repository-root", "base", "head", "output"),
            ["plan"] = Set("repository-root", "policy", "base", "head", "results", "output"),
            ["sanitize-recovery-trx"] = Set("repository-root", "input", "output"),
            ["ci"] = Set("repository-root", "policy", "base", "head", "results", "report-directory"),
        };

    private readonly IReadOnlyDictionary<string, string> _values;

    private CommandArguments(string command, IReadOnlyDictionary<string, string> values)
    {
        Command = command;
        _values = values;
    }

    /// <summary>Gets the command name.</summary>
    public string Command { get; }

    /// <summary>Parses a command and strict key/value options.</summary>
    /// <param name="args">The process arguments.</param>
    /// <returns>The parsed arguments.</returns>
    public static CommandArguments Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length == 0 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "command");
        }

        if (!AllowedOptions.TryGetValue(args[0], out IReadOnlySet<string>? allowedOptions))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "command");
        }

        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int index = 1; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new GateValidationException(GateReason.StatusMismatch, "arguments");
            }

            string key = args[index][2..];
            if (key.Length == 0 || !allowedOptions.Contains(key) || !values.TryAdd(key, args[index + 1]))
            {
                throw new GateValidationException(GateReason.StatusMismatch, key);
            }
        }

        return new CommandArguments(args[0], values);
    }

    /// <summary>Gets a required option.</summary>
    /// <param name="name">The option name without dashes.</param>
    /// <returns>The non-empty option.</returns>
    public string Required(string name)
    {
        if (!_values.TryGetValue(name, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new GateValidationException(GateReason.StatusMismatch, name);
        }

        return value;
    }

    /// <summary>Gets an optional option.</summary>
    /// <param name="name">The option name without dashes.</param>
    /// <returns>The option, when present.</returns>
    public string? Optional(string name) => _values.GetValueOrDefault(name);

    private static IReadOnlySet<string> Set(params string[] values) => values.ToHashSet(StringComparer.Ordinal);
}
