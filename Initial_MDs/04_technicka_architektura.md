# DuplicateFinder – Technická architektura & Pokyny pro Gemini

## Architektura aplikace (MVVM)
Aplikace musí být postavena na vzoru **MVVM (Model-View-ViewModel)**:

```
DuplicateFinder/
├── Models/
│   ├── FileEntry.cs          # Reprezentace jednoho souboru
│   ├── ScanResult.cs         # Výsledek jednoho páru duplicit
│   └── ScanSettings.cs       # Konfigurace skenování
├── ViewModels/
│   ├── MainViewModel.cs      # Logika hlavního okna
│   ├── ResultsViewModel.cs   # Logika zobrazení výsledků
│   └── SettingsViewModel.cs  # Logika nastavení
├── Views/
│   ├── MainWindow.xaml       # Hlavní okno
│   ├── ResultsView.xaml      # Pohled výsledků
│   └── SettingsView.xaml     # Nastavení
├── Services/
│   ├── ScanEngine.cs         # Jádro skenování a hashování
│   ├── FileComparer.cs       # Porovnávací logika
│   ├── RescueCenter.cs       # Správa záchranné složky
│   └── ExportService.cs      # Export do CSV/HTML
└── Helpers/
    ├── HashHelper.cs         # MD5 / SHA-256 výpočet
    └── FileSizeHelper.cs     # Formátování velikostí
```

## Klíčové datové struktury

### FileEntry
```csharp
public class FileEntry
{
    public string FullPath { get; set; }
    public string FileName { get; set; }
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public string Hash { get; set; }          // vypočítán při skenování
    public DuplicateStatus Status { get; set; }
}

public enum DuplicateStatus
{
    ExactDuplicate,       // Shodný hash
    ProbableDuplicate,    // Stejná velikost, podobný název
    Unique                // Pouze v jedné složce
}
```

## Algoritmus detekce duplicit

1. **Krok 1 – Načtení souborů:** Rekurzivně projít obě složky, vytvořit seznam `FileEntry` pro A i B
2. **Krok 2 – Rychlý filtr:** Seskupit soubory dle velikosti – kandidáti na duplicity mají shodnou velikost
3. **Krok 3 – Hashování:** Pro každou skupinu kandidátů vypočítat hash (paralelně)
4. **Krok 4 – Porovnání hashů:** Soubory se shodným hashem = `ExactDuplicate`
5. **Krok 5 – Fuzzy matching:** Zbývající soubory porovnat dle Levenshtein distance názvů → `ProbableDuplicate`
6. **Krok 6 – Výsledky:** Sestavit `ScanResult` seznam a předat do `ResultsViewModel`

## Výkonnostní optimalizace
- Hashovat nejprve první a poslední 4 KB souboru (rychlý pre-check)
- Plný hash počítat pouze u souborů, kde se pre-check shoduje
- Použít `Parallel.ForEach` s `CancellationToken` pro zrušení skenování
- Výsledky zobrazovat průběžně pomocí `ObservableCollection<ScanResult>`

## Pokyny pro Gemini (generování kódu)

### Prioritní pořadí implementace
1. Základní kostra projektu (WinUI 3 nebo WPF, MVVM)
2. `ScanEngine.cs` – skenování a hash výpočet
3. `MainWindow.xaml` + `MainViewModel.cs`
4. `ResultsView.xaml` – tabulka výsledků s barevným rozlišením
5. Akce (mazání, Rescue Center, Undo)
6. Export, nastavení, lokalizace

### Omezení a pravidla
- **Nepoužívat** deprecated API (např. `System.Windows.Forms.FolderBrowserDialog` ve WinUI)
- Veškerý přístup k souborům obalit do `try/catch` s loggingem
- Žádné hardcodované cesty – vše konfigurovatelné
- Unit testy pro `ScanEngine` a `FileComparer` jsou vítány
- Komentáře v kódu psát **česky nebo anglicky**

### Verze 1.0 – Minimální životaschopný produkt (MVP)
Funkce povinné pro MVP:
- [ ] Výběr dvou složek
- [ ] Skenování s hash porovnáním (MD5)
- [ ] Zobrazení výsledků v tabulce
- [ ] Smazání do koše
- [ ] Progress bar
- [ ] Základní filtry (typ souboru, velikost)

Funkce pro verzi 1.1+:
- [ ] Rescue Center
- [ ] Export CSV/HTML
- [ ] Fuzzy matching (pravděpodobné duplicity)
- [ ] Presety a historie skenování
- [ ] Lokalizace (CS/EN)
