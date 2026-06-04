namespace AssetRenamer.Editor
{
    /// <summary>
    /// Outcome of a single rename plan.
    /// </summary>
    public enum RenameStatus
    {
        Ok,
        Unchanged,
        Collision,
        Invalid
    }
}
