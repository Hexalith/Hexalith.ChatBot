namespace Hexalith.ChatBot.Contracts.Messages;

public static class ChatBotDisabledActionReasons
{
    public const string InsufficientAuthority = "insufficient-authority";
    public const string StateNotPermitted = "state-not-permitted";
    public const string DependencyDegraded = "dependency-degraded";
    public const string AwaitingOtherActor = "awaiting-other-actor";
    public const string PolicyBlocked = "policy-blocked";
    public const string UnresolvedParticipant = "unresolved-participant";
    public const string ParticipantDirectoryDegraded = "participant-directory-degraded";
    public const string CandidateRequired = "candidate-required";
    public const string EvidenceExpired = "evidence-expired";
    public const string NotAuthorized = "not-authorized";
    public const string ProjectionPending = "projection-pending";
    public const string TerminalState = "terminal-state";
    public const string AlreadyDecided = "already-decided";
    public const string AlreadyCorrected = "already-corrected";
    public const string DisabledAction = "disabled-action";
}
