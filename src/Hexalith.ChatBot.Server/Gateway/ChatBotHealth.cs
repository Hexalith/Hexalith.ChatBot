using System.Security.Cryptography;
using System.Text.Json;

using Dapr;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Correlation;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.ChatBot.Server.Gateway;

public sealed record ChatBotHealth(string ModuleName, string DaprAppId, string Status);
