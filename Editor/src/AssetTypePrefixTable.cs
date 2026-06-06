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
            => TryResolveRule(assetPath, out PrefixRule rule) ? rule.m_prefix : string.Empty;

        public string ResolveAliases(string assetPath)
            => TryResolveRule(assetPath, out PrefixRule rule) ? rule.m_aliases : string.Empty;

        public bool TryResolveRule(string assetPath, out PrefixRule rule)
        {
            rule = default;
            if (string.IsNullOrEmpty(assetPath)) return false;

            string extension = Path.GetExtension(assetPath).ToLowerInvariant();
            if (TryMatchExtension(extension, out rule)) return true;

            return TryMatchType(AssetDatabase.GetMainAssetTypeAtPath(assetPath), out rule);
        }

        #endregion


        #region Tools and Utilities

        private bool TryMatchExtension(string extension, out PrefixRule match)
        {
            for (int i = 0; i < m_rules.Count; i++)
            {
                var rule = m_rules[i];
                if (rule.m_match != PrefixMatch.Extension) continue;
                if (string.Equals(NormalizeExtension(rule.m_pattern), extension, StringComparison.OrdinalIgnoreCase))
                {
                    match = rule;
                    return true;
                }
            }
            match = default;
            return false;
        }

        private bool TryMatchType(Type type, out PrefixRule match)
        {
            while (type != null)
            {
                for (int i = 0; i < m_rules.Count; i++)
                {
                    var rule = m_rules[i];
                    if (rule.m_match != PrefixMatch.Type) continue;
                    if (string.Equals(rule.m_pattern, type.Name, StringComparison.Ordinal))
                    {
                        match = rule;
                        return true;
                    }
                }
                type = type.BaseType;
            }
            match = default;
            return false;
        }

        private static string NormalizeExtension(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return string.Empty;
            return pattern.StartsWith(".") ? pattern.ToLowerInvariant() : "." + pattern.ToLowerInvariant();
        }

        private static List<PrefixRule> BuildDefaultRules() => new List<PrefixRule>
        {
            new PrefixRule(PrefixMatch.Extension, ".fbx", "SM_", "mesh staticmesh"),
            new PrefixRule(PrefixMatch.Extension, ".obj", "SM_", "mesh staticmesh"),
            new PrefixRule(PrefixMatch.Extension, ".blend", "SM_", "mesh staticmesh"),
            new PrefixRule(PrefixMatch.Extension, ".prefab", "PF_", "prefab"),
            new PrefixRule(PrefixMatch.Extension, ".png", "T_", "texture tex"),
            new PrefixRule(PrefixMatch.Extension, ".tga", "T_", "texture tex"),
            new PrefixRule(PrefixMatch.Extension, ".psd", "T_", "texture tex"),
            new PrefixRule(PrefixMatch.Extension, ".jpg", "T_", "texture tex"),
            new PrefixRule(PrefixMatch.Extension, ".jpeg", "T_", "texture tex"),
            new PrefixRule(PrefixMatch.Extension, ".exr", "T_", "texture tex"),
            new PrefixRule(PrefixMatch.Extension, ".tif", "T_", "texture tex"),
            new PrefixRule(PrefixMatch.Extension, ".wav", "SFX_", "sound audio sfx"),
            new PrefixRule(PrefixMatch.Extension, ".mp3", "SFX_", "sound audio sfx"),
            new PrefixRule(PrefixMatch.Extension, ".ogg", "SFX_", "sound audio sfx"),
            new PrefixRule(PrefixMatch.Extension, ".anim", "A_", "anim animation"),
            new PrefixRule(PrefixMatch.Extension, ".controller", "AC_", "controller animatorcontroller"),
            new PrefixRule(PrefixMatch.Extension, ".mat", "M_", "material mat"),
            new PrefixRule(PrefixMatch.Extension, ".shader", "SH_", "shader"),
            new PrefixRule(PrefixMatch.Extension, ".shadergraph", "SH_", "shader shadergraph"),
            new PrefixRule(PrefixMatch.Extension, ".vfx", "VFX_", "vfx fx"),
            new PrefixRule(PrefixMatch.Type, "Texture", "T_", "texture tex"),
            new PrefixRule(PrefixMatch.Type, "Material", "M_", "material mat"),
            new PrefixRule(PrefixMatch.Type, "Mesh", "SM_", "mesh staticmesh"),
            new PrefixRule(PrefixMatch.Type, "AnimationClip", "A_", "anim animation"),
            new PrefixRule(PrefixMatch.Type, "AudioClip", "SFX_", "sound audio sfx"),
            new PrefixRule(PrefixMatch.Type, "Shader", "SH_", "shader"),
            new PrefixRule(PrefixMatch.Type, "GameObject", "PF_", "prefab")
        };

        #endregion
    }
}
