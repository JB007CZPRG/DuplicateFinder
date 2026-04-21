# Technical Requirements (Tech_Req)

This document summarizes the technologies used and what is required to successfully build and run the DuplicateFinder application.

## Technologies used

### Platform and language

- **C# / .NET:** targeting **.NET 8** (`net8.0-windows`)
- **OS:** Windows 10/11
- **Application type:** **WPF** desktop application (`<UseWPF>true</UseWPF>`)

### Architecture

- **MVVM** (Model–View–ViewModel)
- Bindings, commands, and change notifications via CommunityToolkit

### Key libraries and packages

- **CommunityToolkit.Mvvm** – MVVM (ObservableObject, RelayCommand/AsyncRelayCommand, source-generated properties)
- **xUnit** (in the test project) – unit tests
- **Microsoft.NET.Test.Sdk** + **xunit.runner.visualstudio** + **coverlet.collector** – running tests / collecting coverage

### System APIs and frameworks

- **Hashing:** `System.Security.Cryptography` (MD5)
- **Working with Windows Recycle Bin:** `Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile` (recycle bin)
- **Folder picker dialog:** `Microsoft.Win32.OpenFolderDialog` (Win32 dialog available in modern .NET)

## What is needed to build

### Minimum requirements

- **Windows 10/11**
- **.NET 8 SDK** (or newer compatible)

> Note: The project targets `net8.0-windows` and uses WPF, so building on Linux/macOS does not make sense without a Windows toolchain.

### Recommended tools

- **Visual Studio 2022** (Workload: ".NET desktop development")
  - or alternatively **VS Code** + C# extension + .NET SDK

## Build / Run / Test

Everything can be done from the repository root.

### Restore (usually automatic)

```powershell
 dotnet restore
```

### Build

```powershell
 dotnet build
```

### Run

```powershell
 dotnet run --project .\DuplicateFinder.csproj
```

### Tests

```powershell
 dotnet test
```

## Publish (optional)

Example publish for a self-contained build (Windows x64):

```powershell
 dotnet publish .\DuplicateFinder.csproj -c Release -r win-x64 --self-contained true
```

If you want a smaller output and you have the .NET runtime installed on the target machine, use framework-dependent:

```powershell
 dotnet publish .\DuplicateFinder.csproj -c Release
```

## Notes on dependencies and behavior

- The application works with files on disk and may encounter permission limitations (access denied). Scanning ignores inaccessible items.
- "Rescue Center" stores files in:
  - `%LocalAppData%\DuplicateFinder\RescueCenter`
  - including the manifest `manifest.json`
- Hashing uses MD5 (pre-check + full hash to confirm exact duplicates).