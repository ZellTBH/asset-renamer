using System;

namespace AssetRenamer.Editor
{
    /// <summary>
    /// A single prefix rule: match an asset by file extension (e.g. ".fbx") or by type name
    /// (e.g. "Texture"), then apply the given prefix (e.g. "SM_").
    /// </summary>
    [Serializable]
    public struct PrefixRule
    {
        #region Public

        public PrefixMatch m_match;
        public string m_pattern;
        public string m_prefix;

        #endregion


        #region Main API

        public PrefixRule(PrefixMatch match, string pattern, string prefix)
        {
            m_match = match;
            m_pattern = pattern;
            m_prefix = prefix;
        }

        #endregion
    }
}
