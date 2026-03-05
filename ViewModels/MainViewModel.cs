using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DuplicateFinder.Models;
using DuplicateFinder.Services;
using DuplicateFinder.Helpers;
using Microsoft.Win32;

namespace DuplicateFinder.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ScanEngine _scanEngine = new();
        private readonly RescueCenterService _rescueCenter = new();
        private CancellationTokenSource? _cts;
        private readonly Stack<UndoAction> _undoStack = new();

        // === Složky ===
        [ObservableProperty] private string _folderA = string.Empty;
        [ObservableProperty] private string _folderB = string.Empty;

        // === Progress & Status ===
        [ObservableProperty] private int _progressValue;
        [ObservableProperty] private bool _isScanning;
        [ObservableProperty] private string _statusText = "Připraven";

        // === Nastavení skenování (FR-02) ===
        [ObservableProperty] private ScanSettings _settings = new();

        // === Filtr výsledků ===
        [ObservableProperty] private string _selectedStatusFilter = "Vše";

        // === Rescue Center ===
        [ObservableProperty] private bool _isRescueCenterOpen;

        // === Statistiky ===
        [ObservableProperty] private int _duplicateCount;
        [ObservableProperty] private int _probableCount;
        [ObservableProperty] private int _uniqueCount;
        [ObservableProperty] private string _savedSpace = "0 B";

        public ObservableCollection<FileEntry> Results { get; } = new();
        public ObservableCollection<FileEntry> FilteredResults { get; } = new();
        public ObservableCollection<RescueEntry> RescueEntries { get; } = new();

        public List<string> StatusFilterOptions { get; } = new()
        {
            "Vše", "🔴 Duplicity", "🟡 Pravděpodobné", "🟢 Unikátní"
        };

        public List<string> ComparisonMethods { get; } = new()
        {
            "Hash (MD5)", "Název + velikost", "Pouze název", "Fuzzy matching"
        };

        [ObservableProperty] private string _selectedComparisonMethod = "Hash (MD5)";

        // === Příkazy ===
        public ICommand ScanCommand => new AsyncRelayCommand(ExecuteScanAsync);
        public ICommand CancelCommand => new RelayCommand(CancelScan);
        public ICommand BrowseFolderACommand => new RelayCommand(BrowseFolderA);
        public ICommand BrowseFolderBCommand => new RelayCommand(BrowseFolderB);
        public ICommand DeleteSelectedCommand => new RelayCommand(DeleteSelected);
        public ICommand RescueSelectedCommand => new RelayCommand(RescueSelected);
        public ICommand UndoCommand => new RelayCommand(UndoLastAction, () => _undoStack.Count > 0);
        public ICommand ExportCsvCommand => new RelayCommand(ExportCsv);
        public ICommand ExportHtmlCommand => new RelayCommand(ExportHtml);
        public ICommand SelectAllCommand => new RelayCommand(SelectAllDuplicates);
        public ICommand DeselectAllCommand => new RelayCommand(DeselectAll);
        public ICommand ToggleRescueCenterCommand => new RelayCommand(ToggleRescueCenter);
        public ICommand RestoreFromRescueCommand => new RelayCommand<RescueEntry>(RestoreFromRescue);
        public ICommand DeleteFromRescueCommand => new RelayCommand<RescueEntry>(DeleteFromRescue);
        public ICommand OpenInExplorerCommand => new RelayCommand<FileEntry>(OpenInExplorer);

        public MainViewModel()
        {
            LoadRescueEntries();
        }

        // === Skenování ===
        private async Task ExecuteScanAsync()
        {
            if (string.IsNullOrWhiteSpace(FolderA) || string.IsNullOrWhiteSpace(FolderB)) return;

            Results.Clear();
            FilteredResults.Clear();
            IsScanning = true;
            ProgressValue = 0;
            _cts = new CancellationTokenSource();

            // Aplikovat metodu porovnání ze selectu
            Settings.ComparisonMethod = SelectedComparisonMethod switch
            {
                "Pouze název" => ComparisonMethod.NameOnly,
                "Název + velikost" => ComparisonMethod.NameAndSize,
                "Fuzzy matching" => ComparisonMethod.FuzzyContent,
                _ => ComparisonMethod.Hash
            };

            try
            {
                StatusText = "Skenuji...";
                var progress = new Progress<int>(v => ProgressValue = v);

                var files = await _scanEngine.ScanFoldersAsync(FolderA, FolderB, progress, _cts.Token, Settings);

                foreach (var file in files)
                    Results.Add(file);

                UpdateStatistics();
                ApplyStatusFilter();
                StatusText = $"Dokončeno. Nalezeno {Results.Count} souborů.";
            }
            catch (OperationCanceledException) { StatusText = "Skenování zrušeno."; }
            catch (Exception ex) { StatusText = $"Chyba: {ex.Message}"; }
            finally { IsScanning = false; }
        }

        private void CancelScan() => _cts?.Cancel();

        // === Výběr složek ===
        private void BrowseFolderA()
        {
            var path = BrowseFolder();
            if (path != null) FolderA = path;
        }

        private void BrowseFolderB()
        {
            var path = BrowseFolder();
            if (path != null) FolderB = path;
        }

        private string? BrowseFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Vyberte složku"
            };

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

        // === Akce s duplicitami (FR-05) ===
        private void DeleteSelected()
        {
            var selected = Results.Where(f => f.IsSelected).ToList();
            if (selected.Count == 0) return;

            foreach (var file in selected)
            {
                try
                {
                    FileActionService.SendToRecycleBin(file.FullPath);
                    _undoStack.Push(new UndoAction(UndoActionType.DeletedToRecycleBin, file));
                    Results.Remove(file);
                }
                catch (Exception ex)
                {
                    StatusText = $"Chyba mazání: {ex.Message}";
                }
            }

            UpdateStatistics();
            ApplyStatusFilter();
            StatusText = $"Smazáno {selected.Count} souborů do koše.";
        }

        private void RescueSelected()
        {
            var selected = Results.Where(f => f.IsSelected).ToList();
            if (selected.Count == 0) return;

            int rescued = 0;
            foreach (var file in selected)
            {
                try
                {
                    var entry = _rescueCenter.MoveToRescue(file.FullPath);
                    _undoStack.Push(new UndoAction(UndoActionType.MovedToRescue, file, entry));
                    Results.Remove(file);
                    RescueEntries.Add(entry);
                    rescued++;
                }
                catch (Exception ex)
                {
                    StatusText = $"Chyba Rescue: {ex.Message}";
                }
            }

            UpdateStatistics();
            ApplyStatusFilter();
            StatusText = $"Přesunuto {rescued} souborů do Rescue Center.";
        }

        // === Undo (FR-05: Ctrl+Z) ===
        private void UndoLastAction()
        {
            if (_undoStack.Count == 0) return;

            var action = _undoStack.Pop();
            switch (action.Type)
            {
                case UndoActionType.MovedToRescue when action.RescueEntry != null:
                    if (_rescueCenter.RestoreFile(action.RescueEntry))
                    {
                        RescueEntries.Remove(action.RescueEntry);
                        Results.Add(action.FileEntry);
                        StatusText = "Undo: Soubor obnoven z Rescue Center.";
                    }
                    break;
                case UndoActionType.DeletedToRecycleBin:
                    StatusText = "Undo: Obnovte soubor ručně z koše Windows.";
                    break;
            }

            UpdateStatistics();
            ApplyStatusFilter();
        }

        // === Export (FR-07) ===
        private void ExportCsv()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV soubory (*.csv)|*.csv",
                FileName = $"DuplicateFinder_Report_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                ExportService.ExportToCsv(Results, dialog.FileName);
                StatusText = $"Export CSV uložen: {dialog.FileName}";
            }
        }

        private void ExportHtml()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "HTML soubory (*.html)|*.html",
                FileName = $"DuplicateFinder_Report_{DateTime.Now:yyyyMMdd_HHmm}.html"
            };

            if (dialog.ShowDialog() == true)
            {
                ExportService.ExportToHtml(Results, dialog.FileName, FolderA, FolderB);
                StatusText = $"Export HTML uložen: {dialog.FileName}";
            }
        }

        // === Hromadný výběr ===
        private void SelectAllDuplicates()
        {
            foreach (var file in Results.Where(f => f.Status == DuplicateStatus.ExactDuplicate || f.Status == DuplicateStatus.ProbableDuplicate))
                file.IsSelected = true;
        }

        private void DeselectAll()
        {
            foreach (var file in Results)
                file.IsSelected = false;
        }

        // === Rescue Center ===
        private void ToggleRescueCenter()
        {
            IsRescueCenterOpen = !IsRescueCenterOpen;
            if (IsRescueCenterOpen)
                LoadRescueEntries();
        }

        private void LoadRescueEntries()
        {
            RescueEntries.Clear();
            foreach (var entry in _rescueCenter.GetAllEntries())
                RescueEntries.Add(entry);
        }

        private void RestoreFromRescue(RescueEntry? entry)
        {
            if (entry == null) return;
            if (_rescueCenter.RestoreFile(entry))
            {
                RescueEntries.Remove(entry);
                StatusText = $"Soubor '{entry.FileName}' obnoven na {entry.OriginalPath}";
            }
        }

        private void DeleteFromRescue(RescueEntry? entry)
        {
            if (entry == null) return;
            if (_rescueCenter.PermanentlyDelete(entry))
            {
                RescueEntries.Remove(entry);
                StatusText = $"Soubor '{entry.FileName}' trvale smazán.";
            }
        }

        // === Otevřít v průzkumníku ===
        private void OpenInExplorer(FileEntry? file)
        {
            if (file == null) return;
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{file.FullPath}\"");
            }
            catch { /* ignorovat */ }
        }

        // === Filtrování výsledků ===
        partial void OnSelectedStatusFilterChanged(string value) => ApplyStatusFilter();

        private void ApplyStatusFilter()
        {
            FilteredResults.Clear();

            IEnumerable<FileEntry> filtered = SelectedStatusFilter switch
            {
                "🔴 Duplicity" => Results.Where(f => f.Status == DuplicateStatus.ExactDuplicate),
                "🟡 Pravděpodobné" => Results.Where(f => f.Status == DuplicateStatus.ProbableDuplicate),
                "🟢 Unikátní" => Results.Where(f => f.Status == DuplicateStatus.Unique),
                _ => Results
            };

            foreach (var file in filtered)
                FilteredResults.Add(file);
        }

        // === Statistiky ===
        private void UpdateStatistics()
        {
            DuplicateCount = Results.Count(f => f.Status == DuplicateStatus.ExactDuplicate);
            ProbableCount = Results.Count(f => f.Status == DuplicateStatus.ProbableDuplicate);
            UniqueCount = Results.Count(f => f.Status == DuplicateStatus.Unique);

            long savedBytes = Results
                .Where(f => f.Status == DuplicateStatus.ExactDuplicate)
                .GroupBy(f => f.Hash)
                .Sum(g => g.Skip(1).Sum(f => f.SizeBytes));

            SavedSpace = FileSizeHelper.FormatSize(savedBytes);
        }

        // === Undo model ===
        private record UndoAction(UndoActionType Type, FileEntry FileEntry, RescueEntry? RescueEntry = null);

        private enum UndoActionType
        {
            DeletedToRecycleBin,
            MovedToRescue
        }
    }
}