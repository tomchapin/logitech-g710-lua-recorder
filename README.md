# Logitech Lua Recorder

Windows 11 desktop recorder for building Logitech G-series Lua macros from real keyboard and mouse input.

## Current Version

`0.0.1`

## What It Does

- Records keyboard events, mouse button events, mouse wheel events, screen coordinates, and delays.
- Exports playable Lua scripts for Logitech G-series scripting.
- Reopens app-generated Lua files for editing.
- Supports multi-row selection, bulk cell edits, and multi-row move/delete operations.
- Uses a configurable global function-key hotkey to start and stop recording.

## Project Layout

- `src/LogitechLuaRecorder.App` - WPF desktop application
- `docs/G-seriesLuaAPI.pdf` - original Logitech API reference
- `docs/G-seriesLuaAPI.md` - generated searchable markdown reference
- `scripts/convert_gseries_pdf_to_markdown.py` - PDF-to-markdown extraction helper

## Build

```powershell
dotnet build LogitechLuaRecorder.sln
```

## Notes

- The generated Lua currently targets Logitech G-series scripting behavior used for the G710+ workflow.
- The markdown API reference is auto-extracted from the PDF and may contain formatting artifacts.
