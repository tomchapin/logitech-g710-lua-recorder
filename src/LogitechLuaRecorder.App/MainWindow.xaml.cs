using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using LogitechLuaRecorder.App.Models;
using LogitechLuaRecorder.App.Services;
using LogitechLuaRecorder.App.ViewModels;
using Microsoft.Win32;

namespace LogitechLuaRecorder.App;

public partial class MainWindow : Window
{
    private readonly AppSettingsStore _appSettingsStore = new();
    private readonly MainWindowViewModel _viewModel = new();
    private readonly RawInputRecorderService _recorderService = new();
    private readonly LuaScriptSerializer _luaScriptSerializer = new();
    private AppSettingsData _appSettings = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _appSettings = _appSettingsStore.Load();
        _recorderService.SetToggleHotkey(_appSettings.ToggleHotkeyVirtualKey);
        _viewModel.SetToggleHotkeyDisplay(_recorderService.ToggleHotkeyDisplay);

        SourceInitialized += OnSourceInitialized;
        Closing += OnClosing;
        _recorderService.EventCaptured += OnEventCaptured;
        _recorderService.ToggleRecordingRequested += OnToggleRecordingRequested;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            _recorderService.Attach(source);
            UpdateReadyStatus();
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            e.Cancel = true;
            return;
        }

        _recorderService.Dispose();
    }

    private void OnEventCaptured(object? sender, EditableMacroEvent e)
    {
        Dispatcher.Invoke(() => _viewModel.AddCapturedEvent(e));
    }

    private void OnToggleRecordingRequested(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(ToggleRecording);
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        _viewModel.ResetDocument();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Lua files (*.lua)|*.lua|All files (*.*)|*.*",
            CheckFileExists = true,
            Title = "Open Recorded Lua Macro",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var content = File.ReadAllText(dialog.FileName);
            var document = _luaScriptSerializer.Deserialize(content);
            _viewModel.LoadDocument(document, dialog.FileName);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Open Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SaveDocument(saveAs: false);
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        SaveDocument(saveAs: true);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleRecording_Click(object sender, RoutedEventArgs e)
    {
        ToggleRecording();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsDialog(_recorderService.ToggleHotkeyVirtualKey)
        {
            Owner = this,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _appSettings.ToggleHotkeyVirtualKey = dialog.SelectedHotkeyVirtualKey;
        _appSettingsStore.Save(_appSettings);
        _recorderService.SetToggleHotkey(_appSettings.ToggleHotkeyVirtualKey);
        _viewModel.SetToggleHotkeyDisplay(_recorderService.ToggleHotkeyDisplay);

        if (_viewModel.IsRecording)
        {
            _viewModel.PrepareForRecording(_recorderService.ToggleHotkeyDisplay);
        }
        else
        {
            UpdateReadyStatus();
        }
    }

    private void DeleteSelectedEvent_Click(object sender, RoutedEventArgs e)
    {
        var selectedEvents = GetSelectedEvents();
        if (selectedEvents.Count > 1)
        {
            _viewModel.DeleteEvents(selectedEvents);
            return;
        }

        _viewModel.DeleteSelectedEvent();
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedEvents(-1);
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedEvents(1);
    }

    private void EventsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.SetSelectedEvents(GetSelectedEvents());
    }

    private void EventsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit || e.Row.Item is not EditableMacroEvent currentRow)
        {
            return;
        }

        if (EventsDataGrid.SelectedItems.Count <= 1)
        {
            return;
        }

        if (e.Column is not DataGridBoundColumn boundColumn || boundColumn.Binding is not Binding binding || string.IsNullOrWhiteSpace(binding.Path?.Path))
        {
            return;
        }

        var propertyName = binding.Path.Path;
        Dispatcher.BeginInvoke(() => ApplyEditedValueToSelection(propertyName, currentRow));
    }

    private void SaveDocument(bool saveAs)
    {
        try
        {
            var targetFilePath = _viewModel.CurrentFilePath;
            if (saveAs || string.IsNullOrWhiteSpace(targetFilePath))
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Lua files (*.lua)|*.lua|All files (*.*)|*.*",
                    DefaultExt = ".lua",
                    FileName = $"{_viewModel.MacroName}.lua",
                    Title = "Save Recorded Lua Macro",
                };

                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                targetFilePath = dialog.FileName;
            }

            var output = _luaScriptSerializer.Serialize(_viewModel.BuildDocument());
            File.WriteAllText(targetFilePath!, output);
            _viewModel.SetSavedFilePath(targetFilePath!);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool ConfirmDiscardChanges()
    {
        if (!_viewModel.HasUnsavedChanges)
        {
            return true;
        }

        var response = MessageBox.Show(
            this,
            "This macro has unsaved changes. Discard them?",
            "Unsaved Changes",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return response == MessageBoxResult.Yes;
    }

    private void ToggleRecording()
    {
        if (_viewModel.IsRecording)
        {
            _recorderService.StopRecording();
            _viewModel.FinishRecording(_recorderService.ToggleHotkeyDisplay);
            return;
        }

        _viewModel.PrepareForRecording(_recorderService.ToggleHotkeyDisplay);
        _recorderService.StartRecording();
    }

    private void UpdateReadyStatus()
    {
        _viewModel.StatusText = $"Ready. Press {_recorderService.ToggleHotkeyDisplay} anywhere to start or stop recording.";
    }

    private void MoveSelectedEvents(int direction)
    {
        var selectedEvents = GetSelectedEvents();
        if (selectedEvents.Count == 0)
        {
            return;
        }

        if (_viewModel.MoveEvents(selectedEvents, direction))
        {
            ReselectEvents(selectedEvents);
        }
    }

    private List<EditableMacroEvent> GetSelectedEvents()
    {
        return EventsDataGrid.SelectedItems.OfType<EditableMacroEvent>().ToList();
    }

    private void ReselectEvents(IReadOnlyList<EditableMacroEvent> selectedEvents)
    {
        EventsDataGrid.SelectedItems.Clear();
        foreach (var row in selectedEvents)
        {
            if (EventsDataGrid.Items.Contains(row))
            {
                EventsDataGrid.SelectedItems.Add(row);
            }
        }

        if (selectedEvents.Count > 0)
        {
            EventsDataGrid.ScrollIntoView(selectedEvents[0]);
        }
    }

    private void ApplyEditedValueToSelection(string propertyName, EditableMacroEvent sourceRow)
    {
        var selectedEvents = GetSelectedEvents();
        if (selectedEvents.Count <= 1)
        {
            return;
        }

        var propertyInfo = typeof(EditableMacroEvent).GetProperty(propertyName);
        if (propertyInfo is null || !propertyInfo.CanRead || !propertyInfo.CanWrite)
        {
            return;
        }

        var value = propertyInfo.GetValue(sourceRow);
        foreach (var row in selectedEvents)
        {
            if (ReferenceEquals(row, sourceRow))
            {
                continue;
            }

            propertyInfo.SetValue(row, value);
        }

        _viewModel.StatusText = $"Applied {propertyName} to {selectedEvents.Count} selected rows.";
    }
}
