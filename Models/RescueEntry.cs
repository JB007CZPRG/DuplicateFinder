using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DuplicateFinder.Models
{
    /// <summary>
    /// Záznam souboru v Rescue Center (FR-06)
    /// </summary>
    public partial class RescueEntry : ObservableObject
    {
        [ObservableProperty] private string _originalPath = string.Empty;
        [ObservableProperty] private string _rescuePath = string.Empty;
        [ObservableProperty] private string _fileName = string.Empty;
        [ObservableProperty] private long _sizeBytes;
        [ObservableProperty] private DateTime _movedDate;
        [ObservableProperty] private DateTime _expiresDate; // Datum automatického smazání (30 dní)
    }
}
