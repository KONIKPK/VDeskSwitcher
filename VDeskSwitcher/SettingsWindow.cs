using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace VDeskSwitcher;

internal sealed class SettingsWindow : Window
{
    private bool exiting;

    internal SettingsWindow(Action<string> log)
    {
        Title = "DeskOverview — Nastavenia";
        Width = 500;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.CanMinimize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var panel = new StackPanel { Margin = new Thickness(20) };
        const string label = "Čakanie na okraji pred prepnutím plochy (ms)";
        panel.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap });
        var slider = new Slider
        {
            Minimum = EdgeSettings.MinimumDwellMilliseconds,
            Maximum = EdgeSettings.MaximumDwellMilliseconds,
            Value = EdgeSettings.DwellMilliseconds,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            Margin = new Thickness(0, 12, 0, 8)
        };
        AutomationProperties.SetName(slider, label);
        var value = new TextBlock { Text = $"{EdgeSettings.DwellMilliseconds} ms" };
        slider.ValueChanged += (_, e) =>
        {
            EdgeSettings.SetDwell((int)Math.Round(e.NewValue), log);
            value.Text = $"{EdgeSettings.DwellMilliseconds} ms";
        };
        panel.Children.Add(slider);
        panel.Children.Add(value);
        Content = panel;
        Closing += (_, e) => { if (!exiting) { e.Cancel = true; Hide(); } };
        StateChanged += (_, _) => { if (WindowState == WindowState.Minimized) Hide(); };
    }

    internal void ClosePermanently()
    {
        exiting = true;
        Close();
    }
}
