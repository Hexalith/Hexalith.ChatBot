using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// The deterministic NFR44 completeness report for a sampled set of <see cref="OperationalQueueDiagnostics"/>:
/// how many were sampled, how many render a complete runbook diagnostic, and the workflow-item refs of the ones
/// that do not (the defects). It mechanically encodes the NFR44 observable — "each of N sampled items renders a
/// complete diagnostic; any missing field is a defect".
/// </summary>
/// <param name="Sampled">The number of diagnostics evaluated.</param>
/// <param name="Complete">The number with no missing/placeholder field.</param>
/// <param name="DefectWorkflowItemRefs">The workflow-item refs of the incomplete diagnostics, in input order.</param>
public sealed record RunbookDiagnosticCompletenessReport(
    int Sampled,
    int Complete,
    IReadOnlyList<string> DefectWorkflowItemRefs);
