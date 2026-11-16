# maui-drawing

## Material checkbox

`MaterialCheckBox.Value` is nullable: `false` is unchecked, `true` is checked,
and `null` is indeterminate (a dash). It supports two-way binding by default.
`ValueChanged` provides nullable `OldValue` and `NewValue` values.

```xml
<material:MaterialCheckBox Value="{x:Null}" IsThreeState="True" />
```

Declare `xmlns:material="clr-namespace:MauiMaterial.Controls;assembly=MauiMaterial"`
on the containing XAML element.

`IsThreeState` defaults to `false`, preserving two-state taps; tapping an
indeterminate checkbox in this mode checks it. Set it to `true` to cycle
`false -> true -> null -> false`. An indeterminate value can be assigned
programmatically or through binding in either mode.

**Breaking API change:** `Value`, `IMaterialCheckBox.Value`, and the event
arguments' `OldValue` and `NewValue` are now `bool?` instead of `bool`.
Consumers must handle `null` explicitly, for example `checkbox.Value == true`
when testing whether it is checked.