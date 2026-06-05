namespace AssetRenamer.Editor
{
    /// <summary>
    /// How the trailing number of an asset name is padded when moved to the suffix.
    /// Off leaves numbers untouched. Auto pads every number to the widest one in the current batch.
    /// </summary>
    public enum NumberPadding
    {
        Off,
        OneDigit,
        TwoDigits,
        ThreeDigits,
        Auto
    }
}
