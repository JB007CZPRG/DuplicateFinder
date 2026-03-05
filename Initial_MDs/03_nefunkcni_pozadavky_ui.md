# DuplicateFinder – Nefunkční požadavky & UI Design

## Nefunkční požadavky

### Výkon
- Skenování 100 000 souborů za méně než 60 sekund (na SSD disku)
- Výpočet hashů probíhá paralelně (multi-threading)
- Aplikace nesmí blokovat UI vlákno při skenování

### Bezpečnost
- Žádný soubor nesmí být smazán bez explicitního potvrzení uživatelem
- Permanentní mazání musí vyžadovat potvrzovací dialog s varováním
- Rescue Center musí být aktivní po dobu min. 30 dnů

### Použitelnost
- Aplikace musí být použitelná bez instalace (portable verze)
- Musí fungovat bez administrátorských práv pro běžné složky
- Podpora Windows 10 (build 1903+) a Windows 11

### Lokalizace
- Primární jazyk: **čeština**
- Sekundární jazyk: **angličtina**
- Jazyk volitelný v nastavení

---

## UI Design

### Hlavní okno – layout
```
┌─────────────────────────────────────────────────────────────┐
│  DuplicateFinder            [_] [□] [X]                     │
├─────────────────────────────────────────────────────────────┤
│  [Složka A: C:\Users\...\Dokumenty   ] [Vybrat] [⟳]      │
│  [Složka B: D:\Zálohy\...\Dokumenty  ] [Vybrat] [⟳]      │
├────────────────────┬────────────────────────────────────────┤
│  NASTAVENÍ         │                                        │
│  Metoda: [Hash ▼]  │   [▶ POROVNAT]    [✕ Zrušit]          │
│  Filtry: [Nastavit]│                                        │
│  ☑ Podsložky       │   Progress: ████████░░░░ 64% (ETA 12s)│
├────────────────────┴────────────────────────────────────────┤
│  VÝSLEDKY                                [Filtr: Vše ▼]     │
│ ┌──────────────────────────────────────────────────────────┐│
│ │ # │ Název      │ Vel. │ Datum     │ Hash   │ Stav       ││
│ │ 1 │ foto.jpg   │ 2 MB │ 2026-01-10│ abc123 │ 🔴 Duplicita│
│ │ 2 │ report.docx│ 45 kB│ 2025-12-01│ def456 │ 🟡 Podobný  │
│ │ 3 │ archiv.zip │ 500MB│ 2026-02-01│ ghi789 │ 🟢 Unikátní ││
│ └──────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────┤
│  [☑ Vybrat vše]  [🗑 Smazat] [📦 Rescue] [↩ Undo] [📤 Export]│
├─────────────────────────────────────────────────────────────┤
│  Nalezeno: 142 duplicit │ Unikátních: 56 │ Ušetřeno: 1,2 GB │
└─────────────────────────────────────────────────────────────┘
```

### Barevné schéma (Fluent Design)
- Pozadí: `#F3F3F3` (světlé téma) / `#1C1C1C` (tmavé téma)
- Akcent: `#0078D4` (Windows modrá)
- Duplicita: `#FDE7E9` / červená ikona
- Pravděpodobná duplicita: `#FFF4CE` / žlutá ikona
- Unikátní: `#DFF6DD` / zelená ikona

### Navigace
- Menu / Hamburger panel: Domů, Rescue Center, Historie, Nastavení, O aplikaci
- Klávesové zkratky: `Ctrl+O` (otevřít složku), `F5` (spustit scan), `Ctrl+Z` (undo), `Ctrl+E` (export)
