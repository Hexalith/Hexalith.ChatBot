using System.Drawing;
using System.Text.RegularExpressions;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Inspects the flat declaration rules used by the product stylesheet.</summary>
internal static class CssContract
{
    private static readonly HashSet<string> LiteralColorNames = Enum.GetValues<KnownColor>()
        .Where(value => !Color.FromKnownColor(value).IsSystemColor && value != KnownColor.Transparent)
        .Select(value => value.ToString())
        .Append("rebeccapurple")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Finds declarations for an exact selector in the selected media block.</summary>
    /// <param name="css">The product CSS.</param>
    /// <param name="media">The media condition, including parentheses.</param>
    /// <param name="selector">The complete selector to match, not a substring.</param>
    /// <returns>The selector's declaration blocks, or an empty string if absent.</returns>
    internal static string Declarations(string css, string media, string selector)
    {
        string source = WithoutComments(css);
        string marker = $"@media {media}";
        List<string> declarations = [];
        for (int offset = 0; (offset = source.IndexOf(marker, offset, StringComparison.Ordinal)) >= 0; offset++)
        {
            int start = source.IndexOf('{', offset + marker.Length);
            if (start < 0 || !string.IsNullOrWhiteSpace(source[(offset + marker.Length)..start]))
            {
                continue;
            }

            int depth = 1;
            int end = start + 1;
            while (end < source.Length && depth > 0)
            {
                depth += source[end] switch { '{' => 1, '}' => -1, _ => 0 };
                end++;
            }

            foreach (Match rule in Regex.Matches(source[(start + 1)..(end - 1)], @"(?<selectors>[^{}]+)\{(?<body>[^{}]*)\}"))
            {
                if (rule.Groups["selectors"].Value.Split(',').Any(value => value.Trim() == selector))
                {
                    declarations.Add(rule.Groups["body"].Value);
                }
            }
        }

        return string.Join(';', declarations);
    }

    /// <summary>Resolves the last applicable declaration for an exact property in these flat rules.</summary>
    /// <param name="css">The product CSS.</param>
    /// <param name="media">The media condition.</param>
    /// <param name="selector">The exact selector.</param>
    /// <param name="property">The exact property, excluding shorthand expansion.</param>
    /// <returns>The declaration value, preferring important declarations, or an empty string.</returns>
    internal static string Value(string css, string media, string selector, string property)
    {
        string[] values = Regex.Matches(Declarations(css, media, selector),
                $@"(?:^|;)\s*{Regex.Escape(property)}\s*:\s*(?<value>[^;]+)", RegexOptions.IgnoreCase)
            .Select(match => Regex.Replace(match.Groups["value"].Value.Trim(), @"\s+", " "))
            .ToArray();
        return values.LastOrDefault(value => value.EndsWith("!important", StringComparison.OrdinalIgnoreCase))
            ?? values.LastOrDefault() ?? string.Empty;
    }

    /// <summary>Finds literal palette values only in declarations, excluding comments and URLs.</summary>
    /// <param name="css">The stylesheet to inspect.</param>
    /// <returns>Forbidden literal colors, allowing theme variables and adaptive CSS keywords.</returns>
    internal static IReadOnlyList<string> LiteralColors(string css)
    {
        string source = WithoutComments(css);
        List<string> colors = [];
        foreach (Match rule in Regex.Matches(source, @"\{(?<body>[^{}]*)\}"))
        {
            foreach (Match declaration in Regex.Matches(rule.Groups["body"].Value, @"(?:^|;)\s*(?<property>[\w-]+)\s*:\s*(?<value>[^;]+)"))
            {
                if (!IsColorProperty(declaration.Groups["property"].Value))
                {
                    continue;
                }

                string value = Regex.Replace(declaration.Groups["value"].Value, @"url\([^)]*\)|var\(\s*--[\w-]+", string.Empty, RegexOptions.IgnoreCase);
                value = Regex.Replace(value, "\"[^\"]*\"|'[^']*'", string.Empty);
                foreach (Match token in Regex.Matches(value, @"(?<![\w-])(?:\#(?:[0-9a-f]{8}|[0-9a-f]{6}|[0-9a-f]{4}|[0-9a-f]{3})\b|(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color)\s*\(|[a-z]+\b)", RegexOptions.IgnoreCase))
                {
                    if (token.Value.StartsWith('#') || token.Value.TrimEnd().EndsWith('(') || LiteralColorNames.Contains(token.Value))
                    {
                        colors.Add(token.Value);
                    }
                }
            }
        }

        return colors;
    }

    private static string WithoutComments(string css)
        => Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

    private static bool IsColorProperty(string property)
        => property.StartsWith("--", StringComparison.Ordinal) || Regex.IsMatch(property,
            @"^(?:color|background(?:-.+)?|border(?:-.+)?|outline(?:-.+)?|(?:box|text)-shadow|text-(?:decoration|emphasis)(?:-.+)?|column-rule(?:-.+)?|(?:caret|accent|scrollbar|stop|flood|lighting)-color|fill|stroke(?:-.+)?|filter|-webkit-text-(?:fill|stroke)(?:-.+)?)$",
            RegexOptions.IgnoreCase);
}
