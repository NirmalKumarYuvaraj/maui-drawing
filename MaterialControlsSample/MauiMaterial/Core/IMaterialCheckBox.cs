namespace MauiMaterial.Core;

public interface IMaterialCheckBox
{
    /// <summary>
    /// Gets or sets whether the checkbox is checked (true), unchecked (false), or indeterminate (null).
    /// </summary>
    bool? Value { get; set; }

    /// <summary>
    /// Gets or sets whether taps cycle through unchecked, checked, and indeterminate.
    /// </summary>
    bool IsThreeState { get; set; }
}
