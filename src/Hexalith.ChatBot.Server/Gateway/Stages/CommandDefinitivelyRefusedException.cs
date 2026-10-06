namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Proves EventStore definitively refused the submission (typed domain rejection or back-pressure) without committing
/// it, and that no other external writer was attempted before that submission.
/// </summary>
internal sealed class CommandDefinitivelyRefusedException(Exception innerException)
    : InvalidOperationException(innerException.Message, innerException);
