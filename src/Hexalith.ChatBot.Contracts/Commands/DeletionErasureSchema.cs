using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Validation for the deletion/erasure request spec and run result. Reuses the Story 7.4
/// <see cref="RetentionValidationResult"/> and <see cref="ComplianceAdministrationSchema"/> token helpers — it does
/// NOT introduce a second result type or token validator. Beyond per-field closed-set checks it enforces the AC1
/// behavior-vs-action invariant, the architecture #13 WORM-class invariant, the AC4 no-silent-partial invariant, the
/// AC5 proof invariant, and run-status consistency.
/// </summary>
public static class DeletionErasureSchema
{
    private static readonly IReadOnlySet<string> WormClasses =
        new HashSet<string>([ComplianceRetentionClassIds.AuditRecords], StringComparer.Ordinal);

    public static RetentionValidationResult ValidateRequestSpec(DeletionErasureRequestSpec? spec)
    {
        if (spec?.RequestedDataClassIds is not { Count: > 0 } requested ||
            requested.Count > ComplianceRetentionClassIds.All.Count ||
            !DeletionErasureModes.Contains(spec.Mode) ||
            spec.Scope is null ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(spec.Scope.TenantRef))
        {
            return RetentionValidationResult.Invalid("deletion_request_invalid");
        }

        List<string> errors = [];
        HashSet<string> classes = new(StringComparer.Ordinal);
        foreach (string dataClassId in requested)
        {
            if (!ComplianceRetentionClassIds.All.Contains(dataClassId))
            {
                errors.Add("deletion_class_invalid");
            }
            else if (!classes.Add(dataClassId))
            {
                errors.Add("deletion_class_duplicate");
            }
        }

        foreach (string projectScopeRef in spec.Scope.ProjectScopeRefs ?? [])
        {
            if (!ComplianceAdministrationSchema.IsSafeComplianceToken(projectScopeRef))
            {
                errors.Add("deletion_project_ref_invalid");
            }
        }

        return errors.Count == 0
            ? RetentionValidationResult.Valid
            : new RetentionValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    public static RetentionValidationResult ValidateRunResult(
        DeletionErasureRunResult? result,
        IReadOnlyCollection<string>? requestedDataClassIds = null)
    {
        if (result?.ClassResults is not { Count: > 0 } classResults ||
            !DeletionErasureRunStatuses.Contains(result.RunStatus) ||
            !DeletionErasureModes.Contains(result.Mode) ||
            result.Proof is null ||
            !ComplianceAdministrationSchema.IsSafeFingerprint(result.Proof.ProofFingerprint) ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(result.DeletionRunId) ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(result.CorrelationId) ||
            !ComplianceAdministrationSchema.IsUtc(result.GeneratedAtUtc))
        {
            return RetentionValidationResult.Invalid("deletion_result_invalid");
        }

        List<string> errors = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (DeletionErasureClassResult classResult in classResults)
        {
            ValidateClassResult(classResult, seen, errors);
        }

        // Completeness: every requested class is processed exactly once (duplicates caught above).
        if (requestedDataClassIds is not null)
        {
            foreach (string requested in requestedDataClassIds)
            {
                if (!seen.Contains(requested))
                {
                    errors.Add("deletion_class_unprocessed");
                }
            }
        }

        ValidateProof(result, classResults, errors);

        // Run-status consistency with the per-class statuses.
        if (!string.Equals(result.RunStatus, ExpectedRunStatus(classResults), StringComparison.Ordinal))
        {
            errors.Add("deletion_run_status_inconsistent");
        }

        return errors.Count == 0
            ? RetentionValidationResult.Valid
            : new RetentionValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static void ValidateClassResult(DeletionErasureClassResult classResult, HashSet<string> seen, List<string> errors)
    {
        if (classResult is null)
        {
            errors.Add("deletion_result_invalid");
            return;
        }

        if (!ComplianceRetentionClassIds.All.Contains(classResult.DataClassId))
        {
            errors.Add("deletion_class_invalid");
        }
        else if (!seen.Add(classResult.DataClassId))
        {
            errors.Add("deletion_class_duplicate");
        }

        if (!DataClassDeletionBehaviors.Contains(classResult.DeletionBehavior))
        {
            errors.Add("deletion_behavior_invalid");
        }

        if (!DeletionErasureClassActions.Contains(classResult.Action))
        {
            errors.Add("deletion_action_invalid");
        }

        if (!DeletionErasureClassStatuses.Contains(classResult.Status))
        {
            errors.Add("deletion_status_invalid");
        }

        bool retained = string.Equals(classResult.Action, DeletionErasureClassActions.Retained, StringComparison.Ordinal);
        if (retained)
        {
            if (!DeletionErasureExclusionReasons.Contains(classResult.ExclusionReason))
            {
                errors.Add("deletion_exclusion_reason_invalid");
            }
        }
        else if (!string.IsNullOrEmpty(classResult.ExclusionReason))
        {
            errors.Add("deletion_exclusion_reason_invalid");
        }

        // Architecture #13 WORM: a retain-immutable class — and audit-records by identity — is ALWAYS retained with
        // reason worm-retained; it may never be destroyed.
        bool wormBehavior = string.Equals(classResult.DeletionBehavior, DataClassDeletionBehaviors.RetainImmutable, StringComparison.Ordinal);
        if ((wormBehavior || WormClasses.Contains(classResult.DataClassId)) &&
            (!retained || !string.Equals(classResult.ExclusionReason, DeletionErasureExclusionReasons.WormRetained, StringComparison.Ordinal)))
        {
            errors.Add("deletion_worm_class_destroyed");
        }
        else if (!wormBehavior)
        {
            // Behavior-vs-action invariant for the destructive behaviors, unless authority forced retained/unauthorized.
            string expectedAction = classResult.DeletionBehavior switch
            {
                DataClassDeletionBehaviors.KeyShred => DeletionErasureClassActions.CryptoShredded,
                DataClassDeletionBehaviors.ProjectionTombstone => DeletionErasureClassActions.Tombstoned,
                DataClassDeletionBehaviors.HardDelete => DeletionErasureClassActions.HardDeleted,
                _ => string.Empty,
            };

            bool authorityRetained = retained &&
                string.Equals(classResult.ExclusionReason, DeletionErasureExclusionReasons.Unauthorized, StringComparison.Ordinal);
            if (expectedAction.Length > 0 &&
                !authorityRetained &&
                !string.Equals(classResult.Action, expectedAction, StringComparison.Ordinal))
            {
                errors.Add("deletion_behavior_action_mismatch");
            }
        }
    }

    private static void ValidateProof(
        DeletionErasureRunResult result,
        IReadOnlyList<DeletionErasureClassResult> classResults,
        List<string> errors)
    {
        HashSet<string> destructiveClasses = classResults
            .Where(DeletionErasurePlanner.IsSucceededDestructive)
            .Select(static classResult => classResult.DataClassId)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> proofClasses = new(StringComparer.Ordinal);
        foreach (ErasureProofEntry entry in result.Proof.Entries ?? [])
        {
            if (entry is null ||
                !ComplianceAdministrationSchema.IsSafeComplianceToken(entry.SubjectLocator) ||
                !ComplianceAdministrationSchema.IsSafeComplianceToken(entry.KeyHandle))
            {
                errors.Add("deletion_proof_partial_exposed");
                continue;
            }

            // No-partial-exposure: a proof entry exists only for a succeeded destructive class.
            if (!destructiveClasses.Contains(entry.DataClassId))
            {
                errors.Add("deletion_proof_partial_exposed");
            }

            proofClasses.Add(entry.DataClassId);
        }

        // Every succeeded destructive class must carry exactly one proof entry.
        if (!proofClasses.SetEquals(destructiveClasses))
        {
            errors.Add("deletion_proof_partial_exposed");
        }

        // Proof invariant: the fingerprint covers exactly the carried confirmation set.
        string expected = DeletionErasurePlanner.ComputeProofFingerprint(result.Proof.Entries ?? []);
        if (!string.Equals(result.Proof.ProofFingerprint, expected, StringComparison.Ordinal))
        {
            errors.Add("deletion_proof_partial_exposed");
        }
    }

    private static string ExpectedRunStatus(IReadOnlyList<DeletionErasureClassResult> classResults)
    {
        DeletionErasureClassResult[] actionable = classResults
            .Where(static classResult =>
                !string.Equals(classResult.Action, DeletionErasureClassActions.Retained, StringComparison.Ordinal))
            .ToArray();

        if (actionable.Length == 0)
        {
            return DeletionErasureRunStatuses.Completed;
        }

        int succeeded = actionable.Count(static classResult =>
            string.Equals(classResult.Status, DeletionErasureClassStatuses.Succeeded, StringComparison.Ordinal));

        if (succeeded == actionable.Length)
        {
            return DeletionErasureRunStatuses.Completed;
        }

        return succeeded == 0 ? DeletionErasureRunStatuses.Failed : DeletionErasureRunStatuses.PartialFailure;
    }
}
