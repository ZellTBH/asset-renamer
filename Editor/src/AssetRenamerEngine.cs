using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AssetRenamer.Editor
{
    /// <summary>
    /// Turns a set of asset paths into rename plans (tokenize -> normalize -> format -> prefix), flags
    /// collisions and invalid results, and applies the safe ones through AssetDatabase so GUIDs and
    /// references survive.
    /// </summary>
    public static class AssetRenamerEngine
    {
        #region Main API

        public static List<AssetRenamePlan> BuildPlans(IReadOnlyList<string> assetPaths, RenameOptions options)
        {
            var plans = new List<AssetRenamePlan>();
            if (assetPaths == null) return plans;

            int autoWidth = options.m_numberPadding == NumberPadding.Auto ? ComputeAutoWidth(assetPaths, options) : 0;

            for (int i = 0; i < assetPaths.Count; i++)
                plans.Add(BuildPlan(assetPaths[i], options, autoWidth));

            ResolveCollisions(plans);
            return plans;
        }

        public static bool Apply(AssetRenamePlan plan, out string error)
        {
            error = AssetDatabase.RenameAsset(plan.m_assetPath, plan.m_proposedName);
            return string.IsNullOrEmpty(error);
        }

        public static int ApplyAll(IReadOnlyList<AssetRenamePlan> plans, List<RenameRecord> undo, out int failed)
        {
            int applied = 0;
            failed = 0;

            for (int i = 0; i < plans.Count; i++)
            {
                if (plans[i].m_status != RenameStatus.Ok) continue;

                if (Apply(plans[i], out string error))
                {
                    applied++;
                    undo?.Add(new RenameRecord(TargetPath(plans[i]), plans[i].m_originalName));
                }
                else
                {
                    failed++;
                    Debug.LogError($"[Asset Renamer] Failed to rename '{plans[i].m_originalName}{plans[i].m_extension}': {error}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return applied;
        }

        #endregion


        #region Tools and Utilities

        private static AssetRenamePlan BuildPlan(string assetPath, RenameOptions options, int autoWidth)
        {
            string extension = Path.GetExtension(assetPath);
            string originalName = Path.GetFileNameWithoutExtension(assetPath);

            string body = PrepareBody(assetPath, options, out string prefix);

            string numberDigits = null;
            if (options.m_numberPadding != NumberPadding.Off) body = ExtractTrailingNumber(body, out numberDigits);

            var words = NameTokenizer.Tokenize(body);
            string formatted = NameFormatter.Format(words, options.m_convention);

            if (!string.IsNullOrEmpty(numberDigits)) formatted = AppendNumber(formatted, numberDigits, options.m_convention, options.m_numberPadding, autoWidth);

            string core = string.IsNullOrEmpty(formatted) ? string.Empty : WrapAffixes(formatted, options);

            var plan = new AssetRenamePlan
            {
                m_assetPath = assetPath,
                m_originalName = originalName,
                m_extension = extension,
                m_proposedName = string.IsNullOrEmpty(core) ? string.Empty : prefix + core
            };

            ClassifyPlan(plan, core, originalName);
            return plan;
        }

        private static void ClassifyPlan(AssetRenamePlan plan, string formatted, string originalName)
        {
            if (string.IsNullOrEmpty(formatted))
            {
                plan.m_status = RenameStatus.Invalid;
                plan.m_message = "Empty result after formatting.";
            }
            else if (plan.m_proposedName == originalName)
            {
                plan.m_status = RenameStatus.Unchanged;
                plan.m_message = "Already conforms.";
            }
            else
            {
                plan.m_status = RenameStatus.Ok;
                plan.m_message = string.Empty;
            }
        }

        private static string StripKnownPrefix(string name, string prefix)
            => !string.IsNullOrEmpty(prefix) && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? name.Substring(prefix.Length) : name;

        private static string PrepareBody(string assetPath, RenameOptions options, out string prefix)
        {
            string originalName = Path.GetFileNameWithoutExtension(assetPath);
            prefix = options.m_applyPrefix && options.m_prefixTable != null ? options.m_prefixTable.ResolvePrefix(assetPath) : string.Empty;
            string body = StripKnownPrefix(originalName, prefix);
            body = ApplyFindReplace(body, options);
            if (options.m_normalize) body = NameNormalizer.Normalize(body);
            return body;
        }

        private static string ApplyFindReplace(string body, RenameOptions options)
        {
            if (string.IsNullOrEmpty(options.m_findText)) return body;
            string replacement = (options.m_replaceText ?? string.Empty).Replace("$", "$$");
            return Regex.Replace(body, Regex.Escape(options.m_findText), replacement, RegexOptions.IgnoreCase);
        }

        private static string WrapAffixes(string formatted, RenameOptions options)
        {
            string customPrefix = options.m_customPrefix ?? string.Empty;
            string customSuffix = options.m_customSuffix ?? string.Empty;
            return customPrefix + formatted + customSuffix;
        }

        private static string ExtractTrailingNumber(string body, out string digits)
        {
            digits = null;
            if (string.IsNullOrEmpty(body)) return body;

            int end = -1;
            for (int i = body.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(body[i])) { end = i; break; }
            }
            if (end < 0) return body;

            int start = end;
            while (start > 0 && char.IsDigit(body[start - 1])) start--;

            digits = body.Substring(start, end - start + 1);
            return body.Substring(0, start) + " " + body.Substring(end + 1);
        }

        private static string AppendNumber(string formattedBody, string digits, NamingConvention convention, NumberPadding padding, int autoWidth)
        {
            int width = WidthFor(padding, autoWidth);
            string padded = digits.PadLeft(width, '0');
            string separator = NameFormatter.Separator(convention);
            return string.IsNullOrEmpty(formattedBody) ? padded : formattedBody + separator + padded;
        }

        private static int WidthFor(NumberPadding padding, int autoWidth) => padding switch
        {
            NumberPadding.OneDigit => 1,
            NumberPadding.TwoDigits => 2,
            NumberPadding.ThreeDigits => 3,
            NumberPadding.Auto => System.Math.Max(1, autoWidth),
            _ => 0
        };

        private static int ComputeAutoWidth(IReadOnlyList<string> assetPaths, RenameOptions options)
        {
            int max = 0;
            for (int i = 0; i < assetPaths.Count; i++)
            {
                string body = PrepareBody(assetPaths[i], options, out _);
                ExtractTrailingNumber(body, out string digits);
                if (digits != null && digits.Length > max) max = digits.Length;
            }
            return max;
        }

        private static void ResolveCollisions(List<AssetRenamePlan> plans)
        {
            var targetCounts = new Dictionary<string, int>();
            for (int i = 0; i < plans.Count; i++)
            {
                if (plans[i].m_status != RenameStatus.Ok) continue;
                string key = TargetPath(plans[i]).ToLowerInvariant();
                targetCounts[key] = targetCounts.TryGetValue(key, out int count) ? count + 1 : 1;
            }

            for (int i = 0; i < plans.Count; i++)
            {
                if (plans[i].m_status != RenameStatus.Ok) continue;
                string target = TargetPath(plans[i]);

                if (targetCounts[target.ToLowerInvariant()] > 1)
                {
                    plans[i].m_status = RenameStatus.Collision;
                    plans[i].m_message = "Two assets resolve to the same name.";
                    continue;
                }

                bool taken = AssetDatabase.LoadMainAssetAtPath(target) != null
                             && !string.Equals(target, plans[i].m_assetPath, StringComparison.OrdinalIgnoreCase);
                if (taken)
                {
                    plans[i].m_status = RenameStatus.Collision;
                    plans[i].m_message = "An asset with this name already exists.";
                }
            }
        }

        private static string TargetPath(AssetRenamePlan plan)
        {
            int slash = plan.m_assetPath.LastIndexOf('/');
            string directory = slash >= 0 ? plan.m_assetPath.Substring(0, slash) : string.Empty;
            string fileName = plan.m_proposedName + plan.m_extension;
            return directory.Length > 0 ? directory + "/" + fileName : fileName;
        }

        #endregion
    }
}
