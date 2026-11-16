namespace MauiMaterial.Controls.CheckBox;

/// <summary>
/// Event arguments for when the value of a MaterialCheckBox changes.
/// </summary>
public class CheckBoxValueChangedEventArgs : EventArgs
{
    public CheckBoxValueChangedEventArgs(bool? oldValue, bool? newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>Gets the previous value; null indicates an indeterminate state.</summary>
    public bool? OldValue { get; }

    /// <summary>Gets the new value; null indicates an indeterminate state.</summary>
    public bool? NewValue { get; }
}
