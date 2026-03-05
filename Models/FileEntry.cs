using System;

using CommunityToolkit.Mvvm.ComponentModel;

namespace DuplicateFinder.Models
{
    public enum DuplicateStatus
    {
        ExactDuplicate,       // Shodný hash
        ProbableDuplicate,    // Stejná velikost, podobný název
        Unique                // Pouze v jedné složce
    }

    public partial class FileEntry : ObservableObject
    {
        [ObservableProperty] private string _fullPath = string.Empty;
        [ObservableProperty] private string _fileName = string.Empty;
        [ObservableProperty] private long _sizeBytes;
        [ObservableProperty] private DateTime _lastModified;
        [ObservableProperty] private string _hash = string.Empty; // Vypočítán při skenování
        [ObservableProperty] private DuplicateStatus _status;
        [ObservableProperty] private string _folderSource = string.Empty; // Identifikace, zda je ze Složky A nebo B
        [ObservableProperty] private bool _isSelected; // FR-05: Checkbox pro výběr souborů
    }
}