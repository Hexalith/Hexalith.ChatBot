namespace Hexalith.ChatBot.Server.Operations;

/// <summary>Typed safe rejections; none expose command contents or owner state.</summary>
internal enum CommandOperationConflict
{
    /// <summary>Evaluation succeeded.</summary>
    None,
    /// <summary>The required revision guard is invalid.</summary>
    InvalidConcurrency,
    /// <summary>The lifetime identity is occupied by different semantics.</summary>
    OperationConflict,
    /// <summary>The canonical revision has changed.</summary>
    RevisionConflict,
    /// <summary>The decision slot is already occupied.</summary>
    DecisionConflict,
    /// <summary>The transition is not declared or has invalid facts.</summary>
    InvalidTransition,
    /// <summary>The terminal attempt cannot be retried.</summary>
    RetryNotEligible,
}
