# HD2 Stratagem Macros
 
A lightweight Windows desktop tool for executing Helldivers 2 stratagem input sequences via configurable Numpad hotkeys. \
The application doesn't attach itself to the Helldivers 2 process, so GameGuard will not be triggered.
 
## Features
 
- **GUI Stratagem Picker** — browse and select stratagems from a searchable list with SVG icons
- **Numpad Hotkeys** — trigger full stratagem input sequences in-game using Numpad 1–4
- **Global Keyboard Hooks** — works in the background while Helldivers 2 is in focus
- **JSON-Driven Database** — stratagem definitions are loaded from a bundled JSON database
 
## Installation
 
1. Download the latest `.zip` from [Releases](https://github.com/RasmusHS/HD2StratagemMacros/releases)
2. Extract to a folder of your choice
3. Run `HD2StratagemMacros.exe`
 
No .NET runtime required — the release is self-contained.
 
## Usage
 
1. Open the app and assign a stratagem to each slot (Numpad 1–4) in the same order as they were picked in-game
2. Press the bound Numpad key in-game — the app opens the stratagem menu and enters the full directional sequence automatically
 
## Tech Stack
 
- C# / .NET 10
- WPF
- SharpVectors (SVG icon rendering)
- Global low-level keyboard hooks via Win32 interop
 
## Building from Source
 
```
git clone https://github.com/RasmusHS/HD2StratagemMacros.git
cd HD2StratagemMacros
dotnet build src/GUI/GUI.csproj -c Release
```
 
Or publish a self-contained single-file build:
 
```
dotnet publish src/GUI/GUI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
 
## Requirements
 
- Windows 10/11 (x64)
 
## License
 
[AGPL-3.0](LICENSE.txt)
