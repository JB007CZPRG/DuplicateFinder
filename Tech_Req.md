# Technické požadavky (Tech_Req)

Tento dokument shrnuje použité technologie a to, co je potřeba pro úspěšný build a spuštění aplikace DuplicateFinder.

## Použité technologie

### Platforma a jazyk

- **C# / .NET:** cílení na **.NET 8** (`net8.0-windows`)
- **OS:** Windows 10/11
- **Typ aplikace:** desktopová aplikace **WPF** (`<UseWPF>true</UseWPF>`)

### Architektura

- **MVVM** (Model–View–ViewModel)
- Bindingy, příkazy a notifikace změn přes CommunityToolkit

### Klíčové knihovny a balíčky

- **CommunityToolkit.Mvvm** – MVVM (ObservableObject, RelayCommand/AsyncRelayCommand, source-generované properties)
- **xUnit** (v testovacím projektu) – unit testy
- **Microsoft.NET.Test.Sdk** + **xunit.runner.visualstudio** + **coverlet.collector** – spuštění testů / sběr coverage

### Systémové API a frameworky

- **Hashování:** `System.Security.Cryptography` (MD5)
- **Práce s košem Windows:** `Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile` (recycle bin)
- **Dialog pro výběr složky:** `Microsoft.Win32.OpenFolderDialog` (Win32 dialog dostupný v moderních .NET)

## Co je potřeba pro build

### Minimální požadavky

- **Windows 10/11**
- **.NET 8 SDK** (nebo novější kompatibilní)

> Pozn.: Projekt cílí na `net8.0-windows` a používá WPF, takže build na Linux/macOS nedává smysl bez Windows toolchainu.

### Doporučené nástroje

- **Visual Studio 2022** (Workload: „.NET desktop development“)
  - nebo alternativně **VS Code** + C# rozšíření + .NET SDK

## Build / Run / Test

Vše lze provádět z kořene repozitáře.

### Restore (obvykle automaticky)

```powershell
dotnet restore
```

### Build

```powershell
dotnet build
```

### Spuštění

```powershell
dotnet run --project .\DuplicateFinder.csproj
```

### Testy

```powershell
dotnet test
```

## Publish (volitelné)

Příklad publish pro self-contained build (Windows x64):

```powershell
dotnet publish .\DuplicateFinder.csproj -c Release -r win-x64 --self-contained true
```

Pokud chcete menší výstup a máte na cílovém stroji nainstalovaný .NET runtime, použijte framework-dependent:

```powershell
dotnet publish .\DuplicateFinder.csproj -c Release
```

## Poznámky k závislostem a chování

- Aplikace pracuje se soubory na disku a může narážet na omezení práv (přístup odepřen). Skenování ignoruje nepřístupné položky.
- „Rescue Center“ ukládá soubory do:
  - `%LocalAppData%\DuplicateFinder\RescueCenter`
  - včetně manifestu `manifest.json`
- Hashování používá MD5 (pre-check + plný hash pro potvrzení přesných duplicit).
