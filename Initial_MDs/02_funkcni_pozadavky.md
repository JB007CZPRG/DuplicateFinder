# DuplicateFinder – Funkční požadavky

## FR-01: Výběr složek
- Uživatel musí mít možnost vybrat Složku A a Složku B pomocí:
  - Tlačítka "Vybrat složku" (systémový dialog)
  - Drag & Drop přímo do okna aplikace
- Cesty ke složkám musí být zobrazeny v textovém poli (editovatelném)
- Aplikace musí podporovat UNC cesty (síťové složky)

## FR-02: Konfigurace skenování
- Uživatel volí **metodu porovnání**:
  - `Pouze název souboru`
  - `Název + velikost`
  - `Hash (MD5 / CRC32 / SHA-256)` – přesná duplicita
  - `Podobnost obsahu` – fuzzy matching pro textové soubory a obrázky
- Uživatel může nastavit **filtry**:
  - Typy souborů (wildcard nebo přípona: `*.jpg`, `*.docx`, ...)
  - Minimální / maximální velikost souboru
  - Datum poslední úpravy (od – do)
  - Zahrnout / nezahrnout podsložky (rekurzivní skenování)

## FR-03: Skenování
- Spuštění tlačítkem "Porovnat"
- Zobrazení progress baru s:
  - Procentuálním průběhem
  - Počtem zpracovaných souborů
  - Odhadovaným zbývajícím časem (ETA)
- Možnost skenování přerušit tlačítkem "Zrušit"
- Skenování probíhá na pozadí (UI nesmí zamrzat)

## FR-04: Zobrazení výsledků
- Výsledky zobrazeny ve **dvoupanelovém pohledu** (Side-by-Side):
  - Levý panel: Složka A
  - Pravý panel: Složka B
- Každý soubor obsahuje sloupce: Název, Cesta, Velikost, Datum úpravy, Hash, Stav
- Barevné odlišení stavů:
  - 🔴 Červená – přesná duplicita (shodný hash)
  - 🟡 Žlutá – pravděpodobná duplicita (stejná velikost, podobný název)
  - 🟢 Zelená – unikátní soubor (pouze v jedné složce)
- Možnost filtrovat výsledky podle stavu
- Možnost seřadit dle libovolného sloupce
- Náhled souboru (obrázky, text) při označení řádku

## FR-05: Akce s duplicitami
- Checkbox pro výběr souborů (hromadný výběr)
- Automatický výběr dle pravidel:
  - Ponechat novější / starší soubor
  - Ponechat soubor ve Složce A nebo B
- Dostupné akce:
  - `Přesunout do koše` (výchozí bezpečná akce)
  - `Smazat trvale` (s potvrzovacím dialogem)
  - `Přesunout do Rescue Center` (záloha před smazáním)
  - `Ignorovat` (přidat do výjimek)
  - `Otevřít v průzkumníku`
- Undo poslední akce (Ctrl+Z)

## FR-06: Rescue Center
- Speciální složka pro dočasně zálohou souborů před smazáním
- Zobrazení obsahu Rescue Center se stavem a datem přesunu
- Možnost obnovit soubory zpět nebo trvale smazat

## FR-07: Výsledky a exporty
- Uložení výsledků skenování jako CSV nebo HTML report
- Uložení konfigurace skenování jako pojmenovaný preset
- Načtení uloženého presetu při příštím spuštění
