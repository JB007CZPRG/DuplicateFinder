# DuplicateFinder – Přehled projektu

## Cíl aplikace
Vytvořit desktopovou aplikaci pro Windows, která porovná obsah dvou uživatelem vybraných složek a identifikuje pravděpodobné duplicitní soubory. Aplikace musí být intuitivní, rychlá a bezpečná – nesmí mazat soubory bez explicitního potvrzení uživatelem.

## Technologický stack (doporučení)
- **Jazyk:** C# (.NET 8 nebo novější)
- **UI Framework:** WinUI 3 (Fluent Design) nebo WPF s moderním tématem
- **Hashing:** MD5 / CRC32 / SHA-256 (volitelně)
- **Paralelismus:** async/await + Task Parallel Library pro rychlé skenování
- **Cílová platforma:** Windows 10/11 (x64)

## Hlavní moduly
1. `FolderSelector` – výběr dvou složek
2. `ScanEngine` – skenování, výpočet hashů a detekce duplicit
3. `ResultsView` – zobrazení výsledků ve stromovém/tabulkovém pohledu
4. `ActionManager` – bezpečné akce (smazání, přesunutí, ignorování)
5. `SettingsManager` – konfigurace a uložení presetů

## Slovník pojmů
| Pojem | Definice |
|---|---|
| Duplicitní soubor | Soubor se shodným hashem (MD5/SHA) v obou složkách |
| Pravděpodobná duplicita | Soubor se shodnou velikostí a podobným názvem, ale odlišným hashem |
| Unikátní soubor | Soubor existující pouze v jedné složce |
| Rescue Center | Dočasné úložiště před definitivním smazáním |
