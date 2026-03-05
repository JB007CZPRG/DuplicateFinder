using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DuplicateFinder.Models
{
    /// <summary>
    /// Konfigurace skenování (FR-02)
    /// </summary>
    public partial class ScanSettings : ObservableObject
    {
        [ObservableProperty] private ComparisonMethod _comparisonMethod = ComparisonMethod.Hash;
        [ObservableProperty] private bool _includeSubfolders = true;
        [ObservableProperty] private string _fileTypeFilter = "*.*"; // Wildcard filtr (např. "*.jpg;*.png")
        [ObservableProperty] private long _minFileSize; // Minimální velikost v bajtech (0 = bez omezení)
        [ObservableProperty] private long _maxFileSize; // Maximální velikost v bajtech (0 = bez omezení)
        [ObservableProperty] private DateTime? _dateFrom; // Datum od
        [ObservableProperty] private DateTime? _dateTo; // Datum do
        [ObservableProperty] private string _excludePatterns = ""; // Vzory pro vyloučení (např. ".git;node_modules;bin;obj")
    }

    public enum ComparisonMethod
    {
        NameOnly,           // Pouze název souboru
        NameAndSize,        // Název + velikost
        Hash,               // Hash (MD5) – přesná duplicita
        FuzzyContent        // Fuzzy matching pro textové soubory
    }
}
