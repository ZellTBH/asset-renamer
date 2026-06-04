using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AssetRenamer.Editor
{
    /// <summary>
    /// Cleans messy artist names before formatting: strips diacritics (e -> e), removes copy markers
    /// ("(1)", "copie"/"copy") and lets the tokenizer collapse the remaining whitespace.
    /// </summary>
    public static class NameNormalizer
    {
        #region Main API

        public static string Normalize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;

            string stripped = StripDiacritics(raw);
            return RemoveCopyMarkers(stripped);
        }

        #endregion


        #region Tools and Utilities

        private static string StripDiacritics(string text)
        {
            string decomposed = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);

            foreach (char c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string RemoveCopyMarkers(string text)
        {
            string result = Regex.Replace(text, @"\(\s*\d+\s*\)", " ");
            return Regex.Replace(result, @"\b(copie|copy|copia|kopie)\b", " ", RegexOptions.IgnoreCase);
        }

        #endregion
    }
}
