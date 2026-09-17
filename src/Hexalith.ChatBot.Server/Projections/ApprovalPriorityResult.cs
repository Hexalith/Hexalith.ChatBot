using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// The deterministic, explainable priority result for a single pending approval (Story 7.8, NFR46). All fields are
/// metadata-only safe tokens — never project content, evidence, recipient PII, or command bodies.
/// </summary>
/// <param name="Score">The deterministic priority score; higher sorts first.</param>
/// <param name="Explanation">A safe single-token summary of the contributing dimensions (no spaces, ascii-safe).</param>
/// <param name="GroupKey">The tenant-scoped <c>sha256:</c> fingerprint over <c>(requester × command × project)</c>.</param>
internal sealed record ApprovalPriorityResult(decimal Score, string Explanation, string GroupKey);
