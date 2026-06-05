namespace AssetRenamer.Editor
{
    /// <summary>
    /// One entry of the last applied batch, used to revert: where the asset is now and what its name was before.
    /// </summary>
    [System.Serializable]
    public struct RenameRecord
    {
        #region Public

        public string m_newPath;
        public string m_originalName;

        #endregion


        #region Main API

        public RenameRecord(string newPath, string originalName)
        {
            m_newPath = newPath;
            m_originalName = originalName;
        }

        #endregion
    }
}
