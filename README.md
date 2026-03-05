# DuplicateFinder

Desktopová aplikace pro Windows (WPF, .NET 8), která porovnává obsah dvou složek a pomáhá najít duplicitní a „pravděpodobně duplicitní“ soubory. Důraz je na bezpečné akce (koš / Rescue Center) a plynulé skenování bez zamrzání UI.

## Funkce

- Porovnání **dvou složek** (A/B) včetně rekurze do podsložek
- Metody porovnání:
  - **Hash (MD5)** – přesné duplicity
  - **Název + velikost**
  - **Pouze název**
  - **Fuzzy matching** – podobnost názvů (Levenshtein) *v rámci kandidátů se stejnou velikostí*
- Filtry skenování:
  - filtr typů souborů přes wildcard (např. `*.jpg;*.png;*.pdf`)
  - výjimky (např. `.git;node_modules;bin;obj`)
- Výsledky v tabulce s barevným rozlišením stavů:
  - 🔴 přesná duplicita
  - 🟡 pravděpodobná duplicita
  - 🟢 unikátní soubor
- Hromadné akce:
  - smazání vybraných souborů **do koše**
  - přesun vybraných souborů do **Rescue Center**
  - **Undo** poslední akce (u koše informativně; u Rescue Center umí obnovit)
- Export výsledků do **CSV** a **HTML**
- Otevření souboru v Průzkumníku (vybrání souboru)

## Požadavky

- Windows 10/11
- .NET 8 SDK (nebo novější kompatibilní)

## Rychlý start

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

## Použití (UI)

1. Vyberte **Složku A** a **Složku B**.
2. Zvolte **Metodu** porovnání.
3. Volitelně nastavte:
   - **Filtry** (typy souborů; oddělujte `;`)
   - **Podsložky**
   - **Výjimky** (část cesty / názvu; oddělujte `;`)
4. Klikněte na **▶ POROVNAT**.
5. V „VÝSLEDKY“ použijte filtr (Vše / 🔴 / 🟡 / 🟢) a vyberte položky přes checkbox.
6. Akce:
   - **🗑 Smazat** → přesun do koše Windows
   - **📦 Rescue** → přesun do Rescue Center
   - **📤 CSV / 📤 HTML** → export reportu

### Klávesové zkratky

- `Ctrl+O` – výběr složky A
- `F5` – spustit sken
- `Ctrl+Z` – Undo
- `Ctrl+E` – export HTML

## Rescue Center

Rescue Center je lokální „karanténa“ pro soubory před případným trvalým smazáním.

- Umístění: `%LocalAppData%\DuplicateFinder\RescueCenter`
- Evidence: `manifest.json`
- Expirace: položky se zakládají s dobou uchování **30 dnů** (automatické čištění je dostupné v `RescueCenterService`, ale UI ho aktuálně nespouští samočinně).

## Jak funguje detekce (stručně)

- Aplikace nejdřív načte soubory z obou složek (respektuje filtry).
- Pro výkon seskupuje kandidáty podle **velikosti**.
- V režimu **Hash** používá:
  - rychlý **pre-check hash** (MD5 z prvních a posledních 4 KB),
  - a až poté **plný MD5** pro potvrzení přesných duplicit.
- „Fuzzy matching“ porovnává podobnost názvů (Levenshtein) a označí je jako 🟡.

## Struktura řešení

- `DuplicateFinder` – WPF aplikace (MVVM, CommunityToolkit.Mvvm)
- `DuplicateFinder.Tests` – unit testy (xUnit)

Hlavní vrstvy:

- `Views/` – XAML UI
- `ViewModels/` – MVVM logika a příkazy
- `Services/` – skenování, export, práce se soubory, Rescue Center
- `Models/` – datové modely (výsledek, nastavení)
- `Helpers/` – hashing, formátování velikostí, konvertory

## Známá omezení

- „Fuzzy matching“ je aktuálně **fuzzy podle názvu**, ne podle obsahu.
- Filtry minimální/maximální velikosti a datumů jsou v modelu `ScanSettings`, ale nejsou vystavené v UI.
- Undo pro smazání do koše neobnovuje soubor automaticky (koš spravuje Windows).

## Přispívání

- PR a issues jsou vítané.
- Před odesláním změn spusťte `dotnet test`.

## Licence

Licence není v repozitáři zatím specifikovaná (TBD).
