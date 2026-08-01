using System.Globalization;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Loads and materializes the versioned metadata-only recovery dataset used by the live driver.</summary>
internal sealed class RecoveryValidationDataset
{
    private RecoveryValidationDataset(
        RecoveryValidationDatasetDescriptor descriptor,
        IReadOnlyList<RecoveryDatasetRecord> records,
        IReadOnlyList<ProjectConversationSourceEmailView> sourceRecords,
        IReadOnlyList<AuditEnvelope> auditEnvelopes)
    {
        Descriptor = descriptor;
        Records = records;
        SourceRecords = sourceRecords;
        AuditEnvelopes = auditEnvelopes;
    }

    public RecoveryValidationDatasetDescriptor Descriptor { get; }

    public IReadOnlyList<RecoveryDatasetRecord> Records { get; }

    public IReadOnlyList<ProjectConversationSourceEmailView> SourceRecords { get; }

    public IReadOnlyList<AuditEnvelope> AuditEnvelopes { get; }

    public static RecoveryValidationDataset Load(string path, string tenantRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement root = document.RootElement;
        string datasetRef = RequiredString(root, "datasetRef");
        string version = RequiredString(root, "version");
        int volume = root.GetProperty("volume").GetInt32();
        string projectionSchemaVersion = RequiredString(root, "projectionSchemaVersion");
        string projectionMode = RequiredString(root, "projectionMode");
        string validationPartitionRef = RequiredString(root, "validationPartitionRef");

        List<RecoveryDatasetRecord> sourceMetadata = ReadRecords(root, "sourceRecords", "source", "resourceRef", "sourceVersion");
        List<RecoveryDatasetRecord> wormMetadata = ReadRecords(root, "wormAuditRecords", "worm-audit", "recordRef", "sequence");
        List<RecoveryDatasetRecord> governed = ReadRecords(root, "governedCommands", "governed-command", "commandRef");
        List<RecoveryDatasetRecord> approvals = ReadRecords(root, "approvals", "approval", "approvalRef");
        List<RecoveryDatasetRecord> policies = ReadRecords(root, "policySnapshots", "policy-snapshot", "policyRef");
        List<RecoveryDatasetRecord> attachments = ReadRecords(root, "attachmentMetadata", "attachment-metadata", "attachmentRef");
        RecoveryDatasetRecord[] records = [.. sourceMetadata, .. wormMetadata, .. governed, .. approvals, .. policies, .. attachments];
        if (records.Length != volume)
        {
            throw new InvalidDataException($"Recovery dataset declares volume {volume} but materializes {records.Length} records.");
        }

        RecoveryValidationDatasetDescriptor descriptor = new(
            datasetRef,
            version,
            projectionSchemaVersion,
            validationPartitionRef,
            sourceMetadata.Count,
            wormMetadata.Count,
            governed.Count,
            approvals.Count,
            policies.Count,
            attachments.Count,
            UsesIsolatedValidationStore: string.Equals(
                projectionMode,
                "isolated-validation-store",
                StringComparison.Ordinal));
        ProjectConversationSourceEmailView[] sources = sourceMetadata
            .Select(record => SourceRecord(tenantRef, record))
            .ToArray();
        AuditEnvelope[] audits = wormMetadata
            .OrderBy(static record => record.Sequence)
            .Select(record => AuditRecord(tenantRef, datasetRef, record))
            .ToArray();
        return new RecoveryValidationDataset(descriptor, records, sources, audits);
    }

    private static List<RecoveryDatasetRecord> ReadRecords(
        JsonElement root,
        string propertyName,
        string kind,
        string referenceProperty,
        string? numericProperty = null)
    {
        JsonElement array = root.GetProperty(propertyName);
        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() == 0)
        {
            throw new InvalidDataException($"Recovery dataset category '{propertyName}' must be a non-empty array.");
        }

        return array.EnumerateArray()
            .Select(element => new RecoveryDatasetRecord(
                kind,
                RequiredString(element, referenceProperty),
                RequiredString(element, "structuralState"),
                Sequence: numericProperty is not null &&
                    string.Equals(numericProperty, "sequence", StringComparison.Ordinal)
                    ? element.GetProperty(numericProperty).GetInt64()
                    : null,
                SourceVersion: numericProperty is not null &&
                    string.Equals(numericProperty, "sourceVersion", StringComparison.Ordinal)
                    ? element.GetProperty(numericProperty).GetInt64()
                    : null))
            .ToList();
    }

    private static ProjectConversationSourceEmailView SourceRecord(string tenantRef, RecoveryDatasetRecord record)
        => new(
            tenantRef,
            record.Reference,
            "recovery-dataset-mailbox",
            $"provider-{record.Reference}",
            InternetMessageId: null,
            $"conversation-{record.Reference}",
            SourceThreadId: null,
            DateTimeOffset.Parse("2026-08-01T00:00:00Z", CultureInfo.InvariantCulture),
            SourceSentAtUtc: null,
            SourceCreatedAtUtc: null,
            "UTC",
            "Microsoft 365 mailbox",
            "m365-mailbox",
            record.StructuralState,
            "standard",
            ProjectConversationSourceEmailView.CurrentSchemaVersion,
            record.SourceVersion ?? throw new InvalidDataException("A source record requires sourceVersion."),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static AuditEnvelope AuditRecord(string tenantRef, string datasetRef, RecoveryDatasetRecord record)
        => new(
            tenantRef,
            "recovery-validator",
            "human",
            "RecordGovernedNote",
            record.Reference,
            "allow",
            record.StructuralState,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            DateTimeOffset.Parse("2026-08-01T00:00:01Z", CultureInfo.InvariantCulture)
                .AddTicks(record.Sequence ?? throw new InvalidDataException("A WORM record requires sequence.")),
            "policy-v1",
            [$"dataset:{datasetRef}"],
            IdempotencyKey: null,
            "Received-Proposed",
            "metadata-only",
            "accepted",
            AuditCommitPhase.PostCommit,
            "audit-envelope-v1",
            PredecessorHash: null,
            "api");

    private static string RequiredString(JsonElement element, string propertyName)
    {
        string? value = element.GetProperty(propertyName).GetString();
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidDataException($"Recovery dataset property '{propertyName}' must be non-empty.")
            : value;
    }
}
