using System;

namespace MauiMaterial.Core;

/// <summary>
/// Interface defining the contract for a Material Design switch control, which allows users to toggle between two states: on (true) and off (false). The switch provides a Value property to get or set its current state. This interface can be implemented by any class that wants to provide switch functionality following Material Design guidelines.
/// </summary>
public interface IMaterialSwitch
{
    /// <summary>
    /// Gets or sets a value indicating whether the switch is on (true) or off (false).
    /// </summary>
    bool Value { get; set; }
}
