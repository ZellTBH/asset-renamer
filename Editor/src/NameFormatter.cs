using System;
using System.Collections.Generic;
using System.Text;

namespace AssetRenamer.Editor
{
    /// <summary>
    /// Re-emits a list of lowercase words as a single string following a naming convention. Each convention
    /// is just a per-word casing function plus a separator, so the whole thing is a table-driven switch.
    /// </summary>
    public static class NameFormatter
    {
        #region Main API

        public static string Format(IReadOnlyList<string> words, NamingConvention convention)
        {
            if (words == null || words.Count == 0) return string.Empty;

            return convention switch
            {
                NamingConvention.PascalCase => Join(words, Capitalize, ""),
                NamingConvention.CamelCase => Camel(words, ""),
                NamingConvention.SnakeCase => Join(words, Lower, "_"),
                NamingConvention.KebabCase => Join(words, Lower, "-"),
                NamingConvention.FlatCase => Join(words, Lower, ""),
                NamingConvention.UpperFlatCase => Join(words, Upper, ""),
                NamingConvention.PascalSnakeCase => Join(words, Capitalize, "_"),
                NamingConvention.CamelSnakeCase => Camel(words, "_"),
                NamingConvention.ScreamingSnakeCase => Join(words, Upper, "_"),
                NamingConvention.TrainCase => Join(words, Capitalize, "-"),
                NamingConvention.CobolCase => Join(words, Upper, "-"),
                _ => Join(words, Capitalize, "")
            };
        }

        /// <summary>
        /// The separator a convention places between components (empty for the non-separated styles).
        /// </summary>
        public static string Separator(NamingConvention convention) => convention switch
        {
            NamingConvention.SnakeCase => "_",
            NamingConvention.PascalSnakeCase => "_",
            NamingConvention.CamelSnakeCase => "_",
            NamingConvention.ScreamingSnakeCase => "_",
            NamingConvention.KebabCase => "-",
            NamingConvention.TrainCase => "-",
            NamingConvention.CobolCase => "-",
            _ => ""
        };

        #endregion


        #region Tools and Utilities

        private static string Join(IReadOnlyList<string> words, Func<string, string> casing, string separator)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < words.Count; i++)
            {
                if (i > 0) sb.Append(separator);
                sb.Append(casing(words[i]));
            }
            return sb.ToString();
        }

        private static string Camel(IReadOnlyList<string> words, string separator)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < words.Count; i++)
            {
                if (i > 0) sb.Append(separator);
                sb.Append(i == 0 ? Lower(words[i]) : Capitalize(words[i]));
            }
            return sb.ToString();
        }

        private static string Capitalize(string word) => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word.Substring(1);

        private static string Lower(string word) => word.ToLowerInvariant();

        private static string Upper(string word) => word.ToUpperInvariant();

        #endregion
    }
}
