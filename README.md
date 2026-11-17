# maui-drawing

## Material checkbox

Set `ActiveColor` to customize the checked fill and hover color. If it is unset or
`null`, the checkbox uses the current theme's primary color. The checkmark retains
the theme's on-primary color.

```xaml
<material:MaterialCheckBox Value="True" ActiveColor="#B3261E" />
```

Declare the `material` namespace as
`clr-namespace:MauiMaterial.Controls;assembly=MauiMaterial`.