using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Coarse, fail-safe error-budget burn state for a published SLO (Story 8.3, AC5). Stable machine tokens, never
/// translated. The value is derived only from already-available server-side signals — never count-derived into a
/// fabricated percentage — and defaults to <see cref="Unknown"/> whenever the underlying signal is unavailable,
/// mirroring the Story 8.1/8.2 prefer-no-data-over-fabricated-health doctrine. <see cref="Unknown"/> is the
/// default/first member so a defaulted value is honest no-data rather than a fabricated <see cref="WithinBudget"/>.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<ErrorBudgetBurnState>))]
public enum ErrorBudgetBurnState
{
    [EnumMember(Value = "unknown")]
    Unknown,

    [EnumMember(Value = "within-budget")]
    WithinBudget,

    [EnumMember(Value = "approaching")]
    Approaching,

    [EnumMember(Value = "exhausted")]
    Exhausted,
}
