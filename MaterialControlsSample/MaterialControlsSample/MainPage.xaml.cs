using System.Diagnostics;

namespace MaterialControlsSample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private void materialSwitch_ValueChanged(object sender, MauiMaterial.Controls.Switch.SwitchValueChangedEventArgs e)
	{
		Debug.WriteLine($"[Material] Switch value changed from {e.OldValue} to {e.NewValue}");
	}
}
