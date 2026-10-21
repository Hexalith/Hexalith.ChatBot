namespace Hexalith.ChatBot.Server.Gateway.Admission;

/// <summary>The closed product admission profiles. Only the central catalog selects a profile.</summary>
internal enum ChatBotAdmissionProfile
{
    /// <summary>The MailboxIntake v1 admission contract.</summary>
    MailboxIntake,
    /// <summary>The Workflow v1 admission contract.</summary>
    Workflow,
    /// <summary>The GovernedChat v1 admission contract.</summary>
    GovernedChat,
    /// <summary>The AiRead v1 admission contract.</summary>
    AiRead,
    /// <summary>The AiEffect v1 admission contract.</summary>
    AiEffect,
    /// <summary>The HumanEffect v1 admission contract.</summary>
    HumanEffect,
    /// <summary>The CommandExecution v1 admission contract.</summary>
    CommandExecution,
    /// <summary>The Governance v1 admission contract.</summary>
    Governance,
    /// <summary>The DataRights v1 admission contract.</summary>
    DataRights,
    /// <summary>The ProjectionDelivery v1 admission contract.</summary>
    ProjectionDelivery,
}
