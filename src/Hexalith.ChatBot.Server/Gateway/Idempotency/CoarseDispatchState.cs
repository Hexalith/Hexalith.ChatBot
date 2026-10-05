namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

/// <summary>Durable dispatch fence. Unknown historical reservations cannot be reclaimed.</summary>
internal enum CoarseDispatchState
{
    /// <summary>No durable evidence of whether dispatch started.</summary>
    Unknown = 0,
    /// <summary>Admission owns a bounded lease and external dispatch has not started.</summary>
    Reserved = 1,
    /// <summary>The exact safe response is durable and dispatch may have started.</summary>
    Dispatching = 2,
    /// <summary>Ownership was fenced off before dispatch.</summary>
    Aborted = 3,
}
