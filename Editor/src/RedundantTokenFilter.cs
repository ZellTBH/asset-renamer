using System;
using System.Collections.Generic;

namespace AssetRenamer.Editor
{
    /// <summary>
    /// Removes type words that only repeat the prefix, so "wall_mat" under the M_ prefix becomes "M_Wall"
    /// rather than "M_WallMat". Works in two passes: first it detaches an alias that the original name glued
    /// on as an upper-case acronym ("TorchPF" -> "Torch PF") so the tokenizer can separate it, then it drops
    /// whole tokens that match a known alias. Pure logic, no Unity dependency, so it stays testable.
    /// </summary>
    public static class RedundantTokenFilter
    {
        #region Main API

        /// <summary>
        /// Builds the set of redundant alias words for a resolved prefix: the explicit per-rule aliases plus
        /// the prefix itself (without its trailing separator) when it is at least two characters, so single
        /// letters like "t" or "m" are never stripped (protecting names such as "T-Pose").
        /// </summary>
        public static HashSet<string> BuildAliasSet(string prefix, string aliasSpec)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(aliasSpec))
                foreach (var token in aliasSpec.Split(SEPARATORS, StringSplitOptions.RemoveEmptyEntries))
                    set.Add(token);

            string derived = PrefixToken(prefix);
            if (derived.Length >= 2) set.Add(derived);

            return set;
        }

        /// <summary>
        /// Detaches a trailing alias that the name glued on as an upper-case acronym ("TorchPF" -> "Torch PF")
        /// so the tokenizer splits it out. Operates on the cased body and only touches a clean acronym tail
        /// (upper-case run preceded by a lower-case letter or digit), so "Format" and "FORMAT" are left alone.
        /// </summary>
        public static string SplitGluedAliases(string casedBody, HashSet<string> aliasSet)
        {
            if (string.IsNullOrEmpty(casedBody) || aliasSet == null || aliasSet.Count == 0) return casedBody;

            foreach (var alias in aliasSet)
            {
                if (alias.Length < 2 || casedBody.Length <= alias.Length) continue;
                if (!casedBody.EndsWith(alias, StringComparison.OrdinalIgnoreCase)) continue;

                int start = casedBody.Length - alias.Length;
                if (!IsUpperRun(casedBody, start)) continue;

                char before = casedBody[start - 1];
                if (!char.IsLower(before) && !char.IsDigit(before)) continue;

                return casedBody.Substring(0, start) + " " + casedBody.Substring(start);
            }

            return casedBody;
        }

        /// <summary>
        /// Drops whole tokens that match a known alias. Never returns an empty list: if stripping would
        /// remove every token (e.g. a file literally named "material"), the original tokens are kept so the
        /// result stays a valid name.
        /// </summary>
        public static List<string> Strip(List<string> words, HashSet<string> aliasSet)
        {
            if (words == null || words.Count == 0 || aliasSet == null || aliasSet.Count == 0) return words;

            var kept = new List<string>(words.Count);
            for (int i = 0; i < words.Count; i++)
                if (!aliasSet.Contains(words[i])) kept.Add(words[i]);

            return kept.Count > 0 ? kept : words;
        }

        #endregion


        #region Tools and Utilities

        private static string PrefixToken(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return string.Empty;

            int end = prefix.Length;
            while (end > 0 && !char.IsLetterOrDigit(prefix[end - 1])) end--;
            return prefix.Substring(0, end);
        }

        private static bool IsUpperRun(string text, int start)
        {
            for (int i = start; i < text.Length; i++)
                if (!char.IsLetter(text[i]) || !char.IsUpper(text[i])) return false;
            return true;
        }

        #endregion


        #region Private and Protected

        private static readonly char[] SEPARATORS = { ' ', ',', ';', '\t' };

        #endregion
    }
}
