namespace AssetRenamer.Editor
{
    /// <summary>
    /// Bundles every formatting option the engine consumes when building rename plans. Holds plain data
    /// plus a reference to the project prefix table. Lets BuildPlans take one argument instead of a long
    /// parameter list as options grow.
    /// </summary>
    [System.Serializable]
    public struct RenameOptions
    {
        #region Public

        public NamingConvention m_convention;
        public bool m_normalize;
        public bool m_applyPrefix;
        public bool m_stripRedundantTypeTokens;
        public AssetTypePrefixTable m_prefixTable;
        public NumberPadding m_numberPadding;
        public string m_customPrefix;
        public string m_customSuffix;
        public string m_findText;
        public string m_replaceText;

        #endregion
    }
}
