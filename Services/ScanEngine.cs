using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DuplicateFinder.Models;
using DuplicateFinder.Helpers;

namespace DuplicateFinder.Services
{
    public class ScanEngine
    {
        public async Task<List<FileEntry>> ScanFoldersAsync(
            string folderA, string folderB,
            IProgress<int> progress, CancellationToken cancellationToken,
            ScanSettings? settings = null)
        {
            settings ??= new ScanSettings();
            var allFiles = new ConcurrentBag<FileEntry>();
            int processedCount = 0;

            // Krok 1: Načtení souborů (rekurzivně)
            await Task.Run(() =>
            {
                LoadFilesInFolder(folderA, "A", allFiles, cancellationToken, settings);
                LoadFilesInFolder(folderB, "B", allFiles, cancellationToken, settings);
            }, cancellationToken);

            var fileList = allFiles.ToList();
            int totalFiles = fileList.Count;

            if (totalFiles == 0) return fileList;

            // Krok 2: Rychlý filtr - seskupení podle velikosti
            var sizeGroups = fileList.GroupBy(f => f.SizeBytes).Where(g => g.Count() > 1).ToList();
            var uniqueFiles = fileList.GroupBy(f => f.SizeBytes).Where(g => g.Count() == 1).SelectMany(g => g).ToList();

            foreach (var unique in uniqueFiles)
            {
                unique.Status = DuplicateStatus.Unique;
                ReportProgress(ref processedCount, totalFiles, progress);
            }

            // Krok 3 & 4: Hashování kandidátů a porovnání
            var options = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.ForEach(sizeGroups, options, group =>
            {
                var filesInGroup = group.ToList();

                // Pokud metoda porovnání je pouze NameOnly nebo NameAndSize, přeskočit hashování
                if (settings.ComparisonMethod == ComparisonMethod.NameOnly)
                {
                    ProcessByNameOnly(filesInGroup, ref processedCount, totalFiles, progress);
                    return;
                }

                if (settings.ComparisonMethod == ComparisonMethod.NameAndSize)
                {
                    // Stejná velikost + stejný název = duplicita
                    ProcessByNameAndSize(filesInGroup, ref processedCount, totalFiles, progress);
                    return;
                }

                // Optimalizace: Nejprve pre-check 4KB
                var preCheckGroups = filesInGroup.GroupBy(f => HashHelper.CalculatePreCheckHash(f.FullPath));
                var exactDuplicates = new HashSet<FileEntry>();

                foreach (var preGroup in preCheckGroups)
                {
                    if (preGroup.Count() > 1)
                    {
                        // Pokud se pre-check shoduje, počítáme plný hash
                        var fullHashGroups = preGroup.GroupBy(f => HashHelper.CalculateFullHash(f.FullPath));

                        foreach (var fullGroup in fullHashGroups)
                        {
                            if (fullGroup.Count() > 1)
                            {
                                foreach (var file in fullGroup)
                                {
                                    file.Hash = fullGroup.Key;
                                    file.Status = DuplicateStatus.ExactDuplicate;
                                    exactDuplicates.Add(file);
                                    ReportProgress(ref processedCount, totalFiles, progress);
                                }
                            }
                            else
                            {
                                fullGroup.First().Hash = fullGroup.Key;
                            }
                        }
                    }
                    else
                    {
                        var singleFile = preGroup.First();
                        if (string.IsNullOrEmpty(singleFile.Hash))
                        {
                            singleFile.Hash = preGroup.Key;
                        }
                    }
                }

                // Krok 5: Fuzzy matching (Pravděpodobné duplicity)
                var remainingFiles = filesInGroup.Except(exactDuplicates).ToList();
                var processedRemaining = new HashSet<FileEntry>();

                for (int i = 0; i < remainingFiles.Count; i++)
                {
                    var fileA = remainingFiles[i];
                    if (processedRemaining.Contains(fileA)) continue;

                    var fuzzyMatches = new List<FileEntry> { fileA };

                    for (int j = i + 1; j < remainingFiles.Count; j++)
                    {
                        var fileB = remainingFiles[j];
                        if (processedRemaining.Contains(fileB)) continue;

                        if (AreNamesSimilar(fileA.FileName, fileB.FileName))
                        {
                            fuzzyMatches.Add(fileB);
                        }
                    }

                    if (fuzzyMatches.Count > 1)
                    {
                        foreach (var match in fuzzyMatches)
                        {
                            match.Status = DuplicateStatus.ProbableDuplicate;
                            processedRemaining.Add(match);
                            ReportProgress(ref processedCount, totalFiles, progress);
                        }
                    }
                    else
                    {
                        fileA.Status = DuplicateStatus.Unique;
                        processedRemaining.Add(fileA);
                        ReportProgress(ref processedCount, totalFiles, progress);
                    }
                }
            });

            return fileList;
        }

        private void ProcessByNameOnly(List<FileEntry> filesInGroup, ref int processedCount, int totalFiles, IProgress<int> progress)
        {
            var nameGroups = filesInGroup.GroupBy(f => f.FileName, StringComparer.OrdinalIgnoreCase);
            foreach (var nameGroup in nameGroups)
            {
                bool isDuplicate = nameGroup.Count() > 1;
                foreach (var file in nameGroup)
                {
                    file.Status = isDuplicate ? DuplicateStatus.ExactDuplicate : DuplicateStatus.Unique;
                    ReportProgress(ref processedCount, totalFiles, progress);
                }
            }
        }

        private void ProcessByNameAndSize(List<FileEntry> filesInGroup, ref int processedCount, int totalFiles, IProgress<int> progress)
        {
            // Soubory ve skupině již mají stejnou velikost, tak stačí jen porovnat jména
            var nameGroups = filesInGroup.GroupBy(f => f.FileName, StringComparer.OrdinalIgnoreCase);
            foreach (var nameGroup in nameGroups)
            {
                bool isDuplicate = nameGroup.Count() > 1;
                foreach (var file in nameGroup)
                {
                    file.Status = isDuplicate ? DuplicateStatus.ExactDuplicate : DuplicateStatus.ProbableDuplicate;
                    ReportProgress(ref processedCount, totalFiles, progress);
                }
            }
        }

        private void LoadFilesInFolder(string path, string sourceLabel, ConcurrentBag<FileEntry> collection, CancellationToken cancellationToken, ScanSettings settings)
        {
            try
            {
                var enumOptions = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = settings.IncludeSubfolders,
                    ReturnSpecialDirectories = false
                };

                // Rozdělit filtr na více patterns (např. "*.jpg;*.png")
                var patterns = ParseFileTypeFilter(settings.FileTypeFilter);
                var excludePatterns = ParseExcludePatterns(settings.ExcludePatterns);

                foreach (var pattern in patterns)
                {
                    var files = Directory.EnumerateFiles(path, pattern, enumOptions);
                    foreach (var file in files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Kontrola exclude patterns
                        if (ShouldExclude(file, excludePatterns)) continue;

                        var info = new FileInfo(file);

                        // Filtry velikosti
                        if (settings.MinFileSize > 0 && info.Length < settings.MinFileSize) continue;
                        if (settings.MaxFileSize > 0 && info.Length > settings.MaxFileSize) continue;

                        // Filtry data
                        if (settings.DateFrom.HasValue && info.LastWriteTime < settings.DateFrom.Value) continue;
                        if (settings.DateTo.HasValue && info.LastWriteTime > settings.DateTo.Value) continue;

                        collection.Add(new FileEntry
                        {
                            FullPath = info.FullName,
                            FileName = info.Name,
                            SizeBytes = info.Length,
                            LastModified = info.LastWriteTime,
                            FolderSource = sourceLabel
                        });
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při čtení složky {path}: {ex.Message}");
            }
        }

        private static List<string> ParseFileTypeFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return new List<string> { "*.*" };

            return filter.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Where(p => !string.IsNullOrWhiteSpace(p))
                         .ToList();
        }

        private static List<string> ParseExcludePatterns(string excludePatterns)
        {
            if (string.IsNullOrWhiteSpace(excludePatterns))
                return new List<string>();

            return excludePatterns.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                  .Where(p => !string.IsNullOrWhiteSpace(p))
                                  .ToList();
        }

        private static bool ShouldExclude(string filePath, List<string> excludePatterns)
        {
            if (excludePatterns.Count == 0) return false;

            foreach (var pattern in excludePatterns)
            {
                if (filePath.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private bool AreNamesSimilar(string name1, string name2, int maxDistance = 3)
        {
            if (string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase)) return true;

            string n1 = Path.GetFileNameWithoutExtension(name1).ToLowerInvariant();
            string n2 = Path.GetFileNameWithoutExtension(name2).ToLowerInvariant();

            if (Math.Abs(n1.Length - n2.Length) > maxDistance) return false;

            int[,] d = new int[n1.Length + 1, n2.Length + 1];

            for (int i = 0; i <= n1.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= n2.Length; j++) d[0, j] = j;

            for (int i = 1; i <= n1.Length; i++)
            {
                for (int j = 1; j <= n2.Length; j++)
                {
                    int cost = (n1[i - 1] == n2[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n1.Length, n2.Length] <= maxDistance;
        }

        private void ReportProgress(ref int processed, int total, IProgress<int> progress)
        {
            int current = Interlocked.Increment(ref processed);
            if (progress != null && total > 0)
            {
                int percentage = (int)((current / (double)total) * 100);
                progress.Report(percentage);
            }
        }
    }
}