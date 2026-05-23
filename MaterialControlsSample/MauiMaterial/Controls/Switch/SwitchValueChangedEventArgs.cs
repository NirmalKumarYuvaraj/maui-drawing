using System;

namespace MauiMaterial.Controls.Switch;

/// <summary>
/// Event arguments for when the value of a MaterialSwitch changes, containing the old and new boolean values.
/// </summary>
public class SwitchValueChangedEventArgs : EventArgs
{
    public SwitchValueChangedEventArgs(bool oldValue, bool newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>
    /// Gets the new value of the switch after the change.
    /// </summary>
    public bool NewValue { get; private set; }

    /// <summary>
    /// Gets the previous value of the switch before the change.
    /// </summary>
    public bool OldValue { get; private set; }
}
