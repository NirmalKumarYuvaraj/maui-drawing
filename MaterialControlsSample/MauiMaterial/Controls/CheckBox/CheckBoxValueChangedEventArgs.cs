namespace MauiMaterial.Controls.CheckBox;

/// <summary>
/// Event arguments for when the value of a MaterialCheckBox changes.
/// </summary>
public class CheckBoxValueChangedEventArgs : EventArgs
{
    public CheckBoxValueChangedEventArgs(bool oldValue, bool newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>Gets the previous value of the checkbox before the change.</summary>
    public bool OldValue { get; }

    /// <summary>Gets the new value of the checkbox after the change.</summary>
    public bool NewValue { get; }
}
