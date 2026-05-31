namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Accessible landmark metadata for a governed UI surface.
/// </summary>
/// <param name="Role">Landmark or region role.</param>
/// <param name="AccessibleName">Accessible name exposed for the role.</param>
/// <param name="IsRepeatedWithinSurface">Whether this role can appear more than once in a surface.</param>
public sealed record ChatBotLandmarkContract(
    string Role,
    string AccessibleName,
    bool IsRepeatedWithinSurface)
{
    /// <summary>Gets a value indicating whether the landmark has the required role and accessible name.</summary>
    public bool IsComplete
        => !string.IsNullOrWhiteSpace(Role)
            && !string.IsNullOrWhiteSpace(AccessibleName);

    /// <summary>Returns whether all complete landmarks have unique role/name pairs.</summary>
    /// <param name="landmarks">Landmarks to inspect.</param>
    /// <returns><see langword="true" /> when no role/name pair is duplicated.</returns>
    public static bool HasUniqueAccessibleNames(IEnumerable<ChatBotLandmarkContract>? landmarks)
    {
        if (landmarks is null)
        {
            return false;
        }

        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (ChatBotLandmarkContract landmark in landmarks)
        {
            if (!landmark.IsComplete)
            {
                return false;
            }

            string key = $"{landmark.Role.Trim()}:{landmark.AccessibleName.Trim()}";
            if (!names.Add(key))
            {
                return false;
            }
        }

        return true;
    }
}
