using System.Net.Http.Json;
using System.Text.Json;

namespace Hexalith.ChatBot.Client;

/// <summary>A single audit query filter dimension (e.g. <c>actor</c>, <c>surface</c>, <c>message-id</c>).</summary>
public sealed record ComplianceAuditFilter(string FilterRef, string FilterKey, string ValueRef);
