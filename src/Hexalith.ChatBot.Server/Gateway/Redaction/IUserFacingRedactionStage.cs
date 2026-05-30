using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway.Redaction;

internal interface IUserFacingRedactionStage
{
    ProblemDetails Apply(ProblemDetails problem);
}
