using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Writes deterministic camel-case metadata reports.
/// </summary>
public static class JsonReportWriter
{
    /// <summary>Gets the shared serializer options.</summary>
    public static JsonSerializerOptions SerializerOptions { get; } = CreateOptions();

    /// <summary>Serializes a metadata report.</summary>
    /// <param name="report">The report.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(GateReport report) => JsonSerializer.Serialize(report, SerializerOptions);

    /// <summary>Writes a metadata report, creating its parent directory when necessary.</summary>
    /// <param name="path">The report path.</param>
    /// <param name="report">The report.</param>
    public static void Write(string path, GateReport report)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("Report path has no parent directory."));
        File.WriteAllText(fullPath, Serialize(report));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }
}
