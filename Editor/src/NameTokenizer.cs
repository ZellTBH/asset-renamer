using System.Collections.Generic;
using System.Text;

namespace AssetRenamer.Editor
{
    /// <summary>
    /// Splits an arbitrary name into lowercase words. Breaks on explicit separators (underscore, hyphen,
    /// space and any other non-alphanumeric) and before an uppercase letter that starts a lowercase word
    /// (covers "playerController" -> "player", "controller" and the acronym tail "HTTPRequest" -> "http",
    /// "request"). A lone uppercase not followed by a lowercase (a stray capital like the "L" in "walL3",
    /// or a trailing acronym) is treated as noise and kept attached, so "walL3" resolves to "wall3".
    /// </summary>
    public static class NameTokenizer
    {
        #region Main API

        public static List<string> Tokenize(string raw)
        {
            var words = new List<string>();
            if (string.IsNullOrEmpty(raw)) return words;

            var spaced = InsertBoundaries(raw);
            foreach (var part in spaced.Split(' '))
                if (part.Length > 0) words.Add(part.ToLowerInvariant());

            return words;
        }

        #endregion


        #region Tools and Utilities

        private static string InsertBoundaries(string raw)
        {
            var sb = new StringBuilder(raw.Length * 2);

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];

                if (!char.IsLetterOrDigit(c))
                {
                    sb.Append(' ');
                    continue;
                }

                if (i > 0 && IsWordStart(raw, i)) sb.Append(' ');
                sb.Append(c);
            }

            return sb.ToString();
        }

        private static bool IsWordStart(string raw, int i)
            => char.IsUpper(raw[i]) && i + 1 < raw.Length && char.IsLower(raw[i + 1]);

        #endregion
    }
}
