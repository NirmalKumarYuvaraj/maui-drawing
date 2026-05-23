using System;

namespace MauiMaterial.Core;

public interface IMaterialSwitch
{
    /// <summary>
    /// Gets or sets a value indicating whether the switch is on (true) or off (false).
    /// </summary>
    bool Value { get; set; }
}
