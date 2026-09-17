using System.Text.Json;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The registration produced when a record is redacted: the key handle, the subject, and the appended redaction record.</summary>
internal sealed record AuditRedactionRegistration(
    string TenantRef,
    string SubjectRef,
    string KeyHandle,
    string RedactedRecordLocator,
    WormAuditChainRecord RedactionRecord);
