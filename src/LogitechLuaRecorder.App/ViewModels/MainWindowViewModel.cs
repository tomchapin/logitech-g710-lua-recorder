using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using LogitechLuaRecorder.App.Infrastructure;
using LogitechLuaRecorder.App.Models;

namespace LogitechLuaRecorder.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private string _macroName = "New Macro";
    private string? _currentFilePath;
    private string _statusText = "Ready";
    private int _triggerGKey = 1;
    private string _toggleHotkeyDisplay = "F12";
    private int _selectedCount;
    private bool _hasUnsavedChanges;
    private bool _isRecording;
    private EditableMacroEvent? _selectedEvent;
    private ScreenBoundsData _captureBounds = ScreenBoundsData.FromCurrentSystem();

    public MainWindowViewModel()
    {
        Events.CollectionChanged += OnEventsCollectionChanged;
    }

    public ObservableCollection<EditableMacroEvent> Events { get; } = [];

    public string MacroName
    {
        get => _macroName;
        set
        {
            if (SetProperty(ref _macroName, string.IsNullOrWhiteSpace(value) ? "New Macro" : value.Trim()))
            {
                MarkDirty();
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        private set
        {
            if (SetProperty(ref _currentFilePath, value))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public int TriggerGKey
    {
        get => _triggerGKey;
        set
        {
            if (SetProperty(ref _triggerGKey, Math.Max(1, value)))
            {
                MarkDirty();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string ToggleHotkeyDisplay
    {
        get => _toggleHotkeyDisplay;
        private set
        {
            if (SetProperty(ref _toggleHotkeyDisplay, value))
            {
                OnPropertyChanged(nameof(RecordingToggleButtonText));
                OnPropertyChanged(nameof(HotkeySummary));
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            if (SetProperty(ref _isRecording, value))
            {
                OnPropertyChanged(nameof(RecordingToggleButtonText));
                OnPropertyChanged(nameof(RecordingToggleButtonBackground));
            }
        }
    }

    public EditableMacroEvent? SelectedEvent
    {
        get => _selectedEvent;
        set => SetProperty(ref _selectedEvent, value);
    }

    public int SelectedCount
    {
        get => _selectedCount;
        private set
        {
            if (SetProperty(ref _selectedCount, value))
            {
                OnPropertyChanged(nameof(SelectionSummary));
            }
        }
    }

    public ScreenBoundsData CaptureBounds
    {
        get => _captureBounds;
        private set
        {
            if (SetProperty(ref _captureBounds, value))
            {
                OnPropertyChanged(nameof(CaptureBoundsSummary));
            }
        }
    }

    public string CaptureBoundsSummary => $"Bounds: left {CaptureBounds.Left}, top {CaptureBounds.Top}, width {CaptureBounds.Width}, height {CaptureBounds.Height}";

    public string HotkeySummary => $"Toggle Hotkey: {ToggleHotkeyDisplay}";

    public string SelectionSummary => SelectedCount == 1 ? "1 row selected" : $"{SelectedCount} rows selected";

    public string RecordingToggleButtonText => IsRecording ? $"Stop Recording ({ToggleHotkeyDisplay})" : $"Start Recording ({ToggleHotkeyDisplay})";

    public Brush RecordingToggleButtonBackground => IsRecording
        ? new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26))
        : new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));

    public string WindowTitle
    {
        get
        {
            var fileName = CurrentFilePath is null ? "Unsaved.lua" : Path.GetFileName(CurrentFilePath);
            var dirtyMarker = HasUnsavedChanges ? "*" : string.Empty;
            return $"{dirtyMarker}{MacroName} - {fileName} - Logitech Lua Recorder";
        }
    }

    public void ResetDocument()
    {
        UnsubscribeAllRows();
        Events.Clear();
        _macroName = "New Macro";
        _triggerGKey = 1;
        CaptureBounds = ScreenBoundsData.FromCurrentSystem();
        CurrentFilePath = null;
        SelectedEvent = null;
        SelectedCount = 0;
        HasUnsavedChanges = false;
        StatusText = "New macro ready.";
        OnPropertyChanged(nameof(MacroName));
        OnPropertyChanged(nameof(TriggerGKey));
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void LoadDocument(MacroDocumentData document, string filePath)
    {
        ArgumentNullException.ThrowIfNull(document);

        UnsubscribeAllRows();
        Events.Clear();
        _macroName = document.Name;
        _triggerGKey = document.TriggerGKey;
        CaptureBounds = document.CaptureBounds ?? ScreenBoundsData.FromCurrentSystem();

        foreach (var record in document.Events)
        {
            Events.Add(EditableMacroEvent.FromRecord(record));
        }

        RefreshSequenceNumbers();
        CurrentFilePath = filePath;
        SelectedEvent = Events.FirstOrDefault();
        SelectedCount = SelectedEvent is null ? 0 : 1;
        HasUnsavedChanges = false;
        StatusText = $"Opened {Path.GetFileName(filePath)}.";
        OnPropertyChanged(nameof(MacroName));
        OnPropertyChanged(nameof(TriggerGKey));
        OnPropertyChanged(nameof(WindowTitle));
    }

    public MacroDocumentData BuildDocument()
    {
        return new MacroDocumentData
        {
            Name = MacroName,
            TriggerGKey = Math.Max(1, TriggerGKey),
            CaptureBounds = CaptureBounds,
            Events = Events.Select(x => x.ToRecord()).ToList(),
        };
    }

    public void SetSavedFilePath(string path)
    {
        CurrentFilePath = path;
        HasUnsavedChanges = false;
        StatusText = $"Saved {Path.GetFileName(path)}.";
    }

    public void PrepareForRecording(string toggleHotkeyDisplay = "F12")
    {
        CaptureBounds = ScreenBoundsData.FromCurrentSystem();
        IsRecording = true;
        StatusText = $"Recording armed. Press {toggleHotkeyDisplay} anywhere to stop. The first 400 ms is ignored to avoid capturing the toggle key.";
    }

    public void FinishRecording(string toggleHotkeyDisplay = "F12")
    {
        IsRecording = false;
        StatusText = $"Recording stopped with {toggleHotkeyDisplay}. {Events.Count} events in macro.";
    }

    public void SetToggleHotkeyDisplay(string toggleHotkeyDisplay)
    {
        ToggleHotkeyDisplay = toggleHotkeyDisplay;
    }

    public void SetSelectedEvents(IReadOnlyList<EditableMacroEvent> selectedEvents)
    {
        SelectedCount = selectedEvents.Count;
        SelectedEvent = selectedEvents.FirstOrDefault();
    }

    public void AddCapturedEvent(EditableMacroEvent item)
    {
        ArgumentNullException.ThrowIfNull(item);

        Events.Add(item);
        SelectedEvent = item;
        MarkDirty();
        StatusText = $"Captured {item.Summary}.";
    }

    public void DeleteSelectedEvent()
    {
        if (SelectedEvent is null)
        {
            return;
        }

        var index = Events.IndexOf(SelectedEvent);
        Events.Remove(SelectedEvent);
        RefreshSequenceNumbers();
        SelectedEvent = index >= 0 && index < Events.Count ? Events[index] : Events.LastOrDefault();
        MarkDirty();
        StatusText = "Deleted selected event.";
    }

    public void DeleteEvents(IReadOnlyList<EditableMacroEvent> eventsToDelete)
    {
        if (eventsToDelete.Count == 0)
        {
            return;
        }

        var lookup = new HashSet<EditableMacroEvent>(eventsToDelete);
        for (var index = Events.Count - 1; index >= 0; index--)
        {
            if (lookup.Contains(Events[index]))
            {
                Events.RemoveAt(index);
            }
        }

        RefreshSequenceNumbers();
        SelectedEvent = Events.FirstOrDefault();
        SelectedCount = 0;
        MarkDirty();
        StatusText = eventsToDelete.Count == 1 ? "Deleted 1 selected event." : $"Deleted {eventsToDelete.Count} selected events.";
    }

    public void MoveSelectedEvent(int direction)
    {
        if (SelectedEvent is null)
        {
            return;
        }

        MoveEvents([SelectedEvent], direction);
    }

    public bool MoveEvents(IReadOnlyList<EditableMacroEvent> eventsToMove, int direction)
    {
        if (eventsToMove.Count == 0 || direction == 0)
        {
            return false;
        }

        var selectedLookup = new HashSet<EditableMacroEvent>(eventsToMove);
        var reordered = Events.ToList();

        if (direction < 0)
        {
            for (var index = 1; index < reordered.Count; index++)
            {
                if (selectedLookup.Contains(reordered[index]) && !selectedLookup.Contains(reordered[index - 1]))
                {
                    (reordered[index - 1], reordered[index]) = (reordered[index], reordered[index - 1]);
                }
            }
        }
        else
        {
            for (var index = reordered.Count - 2; index >= 0; index--)
            {
                if (selectedLookup.Contains(reordered[index]) && !selectedLookup.Contains(reordered[index + 1]))
                {
                    (reordered[index], reordered[index + 1]) = (reordered[index + 1], reordered[index]);
                }
            }
        }

        if (reordered.SequenceEqual(Events))
        {
            return false;
        }

        Events.Clear();
        foreach (var row in reordered)
        {
            Events.Add(row);
        }

        RefreshSequenceNumbers();
        SelectedEvent = eventsToMove.FirstOrDefault();
        MarkDirty();
        StatusText = eventsToMove.Count == 1 ? "Moved selected event." : $"Moved {eventsToMove.Count} selected events.";
        return true;
    }

    private void OnEventsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (EditableMacroEvent row in e.OldItems)
            {
                UnsubscribeRow(row);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (EditableMacroEvent row in e.NewItems)
            {
                SubscribeRow(row);
            }
        }

        RefreshSequenceNumbers();
    }

    private void SubscribeRow(EditableMacroEvent row)
    {
        row.PropertyChanged -= OnRowPropertyChanged;
        row.PropertyChanged += OnRowPropertyChanged;
    }

    private void UnsubscribeRow(EditableMacroEvent row)
    {
        row.PropertyChanged -= OnRowPropertyChanged;
    }

    private void UnsubscribeAllRows()
    {
        foreach (var row in Events)
        {
            row.PropertyChanged -= OnRowPropertyChanged;
        }
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(EditableMacroEvent.SequenceNumber))
        {
            MarkDirty();
        }
    }

    private void RefreshSequenceNumbers()
    {
        for (var index = 0; index < Events.Count; index++)
        {
            Events[index].SequenceNumber = index + 1;
        }
    }

    private void MarkDirty()
    {
        HasUnsavedChanges = true;
    }
}
