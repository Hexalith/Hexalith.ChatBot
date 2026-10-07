namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>Signals authority lapse before any external effect was attempted.</summary>
internal sealed class ChatBotAuthorityLapsedException() : Exception("The bound authority is no longer current.");
