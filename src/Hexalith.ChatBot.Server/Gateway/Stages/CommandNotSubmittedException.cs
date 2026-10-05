namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>Proves dispatch failed before attempting any external write.</summary>
internal sealed class CommandNotSubmittedException(Exception innerException)
    : InvalidOperationException(innerException.Message, innerException);
