using System.Text.Json;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ProjectConversationCursorPosition(DateTimeOffset OccurredAt, string ItemId)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ToProtectedPosition()
        => JsonSerializer.Serialize(new CursorPositionPayload(OccurredAt.UtcTicks, ItemId), JsonOptions);

    public static bool TryParse(string? value, out ProjectConversationCursorPosition? position)
    {
        position = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        try
        {
            CursorPositionPayload? payload = JsonSerializer.Deserialize<CursorPositionPayload>(value, JsonOptions);
            if (payload is null || string.IsNullOrWhiteSpace(payload.ItemId))
            {
                return false;
            }

            position = new ProjectConversationCursorPosition(new DateTimeOffset(payload.OccurredAtUtcTicks, TimeSpan.Zero), payload.ItemId);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record CursorPositionPayload(long OccurredAtUtcTicks, string ItemId);
}
