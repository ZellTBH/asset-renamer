using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetRenamer.Editor
{
    /// <summary>
    /// Project-defined mapping from asset type or file extension to a name prefix (e.g. Texture -> T_,
    /// prefab -> PF_). Editor-only labelling aid used by the Asset Renamer window. Extension rules are
    /// evaluated before type rules because they are more specific (a .fbx and a .prefab share the
    /// GameObject main type, so type alone cannot tell them apart).
    /// </summary>
    [CreateAssetMenu(fileName = "AssetTypePrefixTable", menuName = "Asset Renamer/Type Prefix Table")]
    public class AssetTypePrefixTable : ScriptableObject
    {
        #region Public

        [Header("Prefix Rules")]
        [Tooltip("Ordered prefix rules. Extension rules are evaluated before type rules.")]
        public List<PrefixRule> m_rules = new List<PrefixRule>();

        #endregion


        #region Unity API

        private void Reset() => LoadDefaults();

        #endregion


        #region Main API

        public void LoadDefaults() => m_rules = BuildDefaultRules();

        /// <summary>
        /// Resolves the prefix for an asset. Returns an empty string when no rule matches.
        /// </summary>
        public string ResolvePrefix(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return string.Empty;

            string extension = Path.GetExtension(assetPath).ToLowerInvariant();
            string byExtension = MatchExtension(extension);
            if (!string.IsNullOrEmpty(byExtension)) return byExtension;

            return MatchType(AssetDatabase.GetMainAssetTypeAtPath(assetPath));
        }

        #endregion


        #region Tools and Utilities

        private string MatchExtension(string extension)
        {
            for (int i = 0; i < m_rules.Count; i++)
            {
                var rule = m_rules[i];
                if (rule.m_match != PrefixMatch.Extension) continue;
                if (string.Equals(NormalizeExtension(rule.m_pattern), extension, StringComparison.OrdinalIgnoreCase))
                    return rule.m_prefix;
            }
            return string.Empty;
        }

        private string MatchType(Type type)
        {
            while (type != null)
            {
                for (int i = 0; i < m_rules.Count; i++)
                {
                    var rule = m_rules[i];
                    if (rule.m_match != PrefixMatch.Type) continue;
                    if (string.Equals(rule.m_pattern, type.Name, StringComparison.Ordinal))
                        return rule.m_prefix;
                }
                type = type.BaseType;
            }
            return string.Empty;
        }

        private static string NormalizeExtension(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return string.Empty;
            return pattern.StartsWith(".") ? pattern.ToLowerInvariant() : "." + pattern.ToLowerInvariant();
        }

        private static List<PrefixRule> BuildDefaultRules() => new List<PrefixRule>
        {
            new PrefixRule(PrefixMatch.Extension, ".fbx", "SM_"),
            new PrefixRule(PrefixMatch.Extension, ".obj", "SM_"),
            new PrefixRule(PrefixMatch.Extension, ".blend", "SM_"),
            new PrefixRule(PrefixMatch.Extension, ".prefab", "PF_"),
            new PrefixRule(PrefixMatch.Extension, ".png", "T_"),
            new PrefixRule(PrefixMatch.Extension, ".tga", "T_"),
            new PrefixRule(PrefixMatch.Extension, ".psd", "T_"),
            new PrefixRule(PrefixMatch.Extension, ".jpg", "T_"),
            new PrefixRule(PrefixMatch.Extension, ".jpeg", "T_"),
            new PrefixRule(PrefixMatch.Extension, ".exr", "T_"),
            new PrefixRule(PrefixMatch.Extension, ".tif", "T_"),
            new PrefixRule(PrefixMatch.Extension, ".wav", "SFX_"),
            new PrefixRule(PrefixMatch.Extension, ".mp3", "SFX_"),
            new PrefixRule(PrefixMatch.Extension, ".ogg", "SFX_"),
            new PrefixRule(PrefixMatch.Extension, ".anim", "A_"),
            new PrefixRule(PrefixMatch.Extension, ".controller", "AC_"),
            new PrefixRule(PrefixMatch.Extension, ".mat", "M_"),
            new PrefixRule(PrefixMatch.Extension, ".shader", "SH_"),
            new PrefixRule(PrefixMatch.Extension, ".shadergraph", "SH_"),
            new PrefixRule(PrefixMatch.Extension, ".vfx", "VFX_"),
            new PrefixRule(PrefixMatch.Type, "Texture", "T_"),
            new PrefixRule(PrefixMatch.Type, "Material", "M_"),
            new PrefixRule(PrefixMatch.Type, "Mesh", "SM_"),
            new PrefixRule(PrefixMatch.Type, "AnimationClip", "A_"),
            new PrefixRule(PrefixMatch.Type, "AudioClip", "SFX_"),
            new PrefixRule(PrefixMatch.Type, "Shader", "SH_"),
            new PrefixRule(PrefixMatch.Type, "GameObject", "PF_")
        };

        #endregion
    }
}
