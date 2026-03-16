using System.Windows;
using LogitechLuaRecorder.App.Models;
using LogitechLuaRecorder.App.Services;

namespace LogitechLuaRecorder.App;

public partial class SettingsDialog : Window
{
    public SettingsDialog(int currentHotkeyVirtualKey)
    {
        InitializeComponent();
        HotkeyComboBox.ItemsSource = RecordingHotkeyCatalog.GetSupportedHotkeys();
        HotkeyComboBox.SelectedItem = RecordingHotkeyCatalog
            .GetSupportedHotkeys()
            .FirstOrDefault(x => x.VirtualKey == RecordingHotkeyCatalog.NormalizeOrDefault(currentHotkeyVirtualKey));
    }

    public int SelectedHotkeyVirtualKey =>
        (HotkeyComboBox.SelectedItem as HotkeyOption)?.VirtualKey ?? 0x7B;

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
