from __future__ import annotations

import argparse
import re
import zlib
from pathlib import Path

SECTION_HEADINGS = {
    "Overview",
    "Reference",
    "Functions",
    "Standard Lua 5.1 Libraries",
    "Appendix A",
}

FUNCTION_HEADINGS = {
    "OnEvent",
    "GetMKeyState",
    "SetMKeyState",
    "Sleep",
    "OutputLogMessage",
    "GetRunningTime",
    "GetDate",
    "ClearLog",
    "PressKey",
    "ReleaseKey",
    "PressAndReleaseKey",
    "IsModifierPressed",
    "PressMouseButton",
    "ReleaseMouseButton",
    "PressAndReleaseMouseButton",
    "IsMouseButtonPressed",
    "MoveMouseTo",
    "MoveMouseWheel",
    "MoveMouseRelative",
    "MoveMouseToVirtual",
    "GetMousePosition",
    "OutputLCDMessage",
    "ClearLCD",
    "PlayMacro",
    "AbortMacro",
    "IsKeyLockOn",
    "SetBacklightColor",
    "OutputDebugMessage",
    "SetMouseDPITable",
    "SetMouseDPITableIndex",
    "EnablePrimaryMouseButtonEvents",
    "SetSteeringWheelProperty",
}


def decode_pdf_literal(text: str) -> str:
    result: list[str] = []
    index = 0
    while index < len(text):
        char = text[index]
        if char != "\\":
            result.append(char)
            index += 1
            continue

        index += 1
        if index >= len(text):
            break

        escaped = text[index]
        mapping = {
            "n": "\n",
            "r": "\r",
            "t": "\t",
            "b": "\b",
            "f": "\f",
            "(": "(",
            ")": ")",
            "\\": "\\",
        }
        if escaped in mapping:
            result.append(mapping[escaped])
        elif escaped.isdigit():
            octal = escaped
            for _ in range(2):
                if index + 1 < len(text) and text[index + 1].isdigit():
                    index += 1
                    octal += text[index]
                else:
                    break
            result.append(chr(int(octal, 8)))
        else:
            result.append(escaped)
        index += 1

    return "".join(result)


def extract_strings(segment: str) -> list[str]:
    values: list[str] = []
    index = 0
    while index < len(segment):
        if segment[index] == "(":
            index += 1
            depth = 1
            buffer: list[str] = []
            while index < len(segment) and depth > 0:
                char = segment[index]
                if char == "\\":
                    if index + 1 < len(segment):
                        buffer.append(char)
                        index += 1
                        buffer.append(segment[index])
                    else:
                        buffer.append(char)
                elif char == "(":
                    depth += 1
                    buffer.append(char)
                elif char == ")":
                    depth -= 1
                    if depth > 0:
                        buffer.append(char)
                else:
                    buffer.append(char)
                index += 1
            values.append(decode_pdf_literal("".join(buffer)))
            continue

        if segment[index] == "<":
            end_index = segment.find(">", index + 1)
            if end_index != -1:
                hex_text = segment[index + 1:end_index]
                if all(character in "0123456789abcdefABCDEF \t\r\n" for character in hex_text):
                    packed = "".join(character for character in hex_text if not character.isspace())
                    try:
                        values.append(bytes.fromhex(packed).decode("utf-16-be"))
                    except Exception:
                        try:
                            values.append(bytes.fromhex(packed).decode("latin1"))
                        except Exception:
                            pass
                    index = end_index + 1
                    continue

        index += 1

    return values


def extract_lines(pdf_path: Path) -> list[str]:
    raw_pdf = pdf_path.read_bytes()
    lines: list[str] = []
    for match in re.finditer(rb"stream\r?\n(.*?)\r?\nendstream", raw_pdf, re.S):
        try:
            stream_text = zlib.decompress(match.group(1)).decode("latin1", "ignore")
        except Exception:
            continue

        if "BT" not in stream_text or ("TJ" not in stream_text and "Tj" not in stream_text):
            continue

        for raw_line in stream_text.splitlines():
            if "TJ" not in raw_line and "Tj" not in raw_line:
                continue

            extracted = "".join(extract_strings(raw_line))
            normalized = normalize_line(extracted)
            if normalized:
                lines.append(normalized)

    return lines


def normalize_line(line: str) -> str:
    line = line.replace("\x00", "").strip()
    line = re.sub(r"\s+", " ", line)
    line = line.replace("G - series", "G-series")
    line = line.replace("G -", "G-")
    line = line.replace(" - series", "-series")
    return line.strip()


def filter_lines(lines: list[str]) -> list[str]:
    filtered: list[str] = []
    for line in lines:
        if not line:
            continue
        if re.fullmatch(r"[. ]+", line):
            continue
        if line.startswith("...."):
            continue
        filtered.append(line)
    return filtered


def render_markdown(lines: list[str]) -> str:
    markdown_lines = [
        "# G-series Lua API",
        "",
        "> Generated from `G-seriesLuaAPI.pdf` by decoding embedded PDF text streams.",
        "> Formatting is automated, so the wording should be trusted more than the layout.",
        "",
    ]

    previous = ""
    for line in lines:
        if line == previous and line in {"Contents", "Reference", "Overview"}:
            continue

        if re.fullmatch(r"\d+", line):
            markdown_lines.extend([f"> Page {line}", ""])
        elif line in SECTION_HEADINGS:
            markdown_lines.extend([f"## {line}", ""])
        elif line in FUNCTION_HEADINGS:
            markdown_lines.extend([f"### {line}", ""])
        else:
            markdown_lines.append(line)
        previous = line

    markdown_lines.append("")
    return "\n".join(markdown_lines)


def main() -> None:
    parser = argparse.ArgumentParser(description="Convert the G-series Lua API PDF into searchable markdown.")
    parser.add_argument("pdf_path", type=Path)
    parser.add_argument("markdown_path", type=Path)
    args = parser.parse_args()

    lines = extract_lines(args.pdf_path)
    markdown = render_markdown(filter_lines(lines))
    args.markdown_path.write_text(markdown, encoding="utf-8")


if __name__ == "__main__":
    main()
