namespace AssetRenamer.Editor
{
    /// <summary>
    /// The rename outcome computed for a single asset: where it is, what it would become, and whether
    /// the change is safe to apply.
    /// </summary>
    public class AssetRenamePlan
    {
        #region Public

        public string m_assetPath;
        public string m_originalName;
        public string m_extension;
        public string m_proposedName;
        public RenameStatus m_status;
        public string m_message;

        #endregion
    }
}
