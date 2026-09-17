using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal sealed record ProjectAiContextPackageAssemblyRequest(
    string TenantId,
    string ProjectId,
    IReadOnlyList<ProjectConversationItemView> Items,
    string CorrelationId);
