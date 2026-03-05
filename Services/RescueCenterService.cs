using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DuplicateFinder.Models;

namespace DuplicateFinder.Services
{
    /// <summary>
    /// Rescue Center – záchranná složka pro dočasnou zálohu souborů (FR-06)
    /// Soubory jsou přesunuty sem místo trvalého smazání a uchovány min. 30 dní.
    /// </summary>
    public class RescueCenterService
    {
        private readonly string _rescueFolder;
        private readonly string _manifestPath;
        private List<RescueEntry> _entries = new();

        public RescueCenterService()
        {
            _rescueFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DuplicateFinder", "RescueCenter");

            _manifestPath = Path.Combine(_rescueFolder, "manifest.json");

            Directory.CreateDirectory(_rescueFolder);
            LoadManifest();
        }

        /// <summary>
        /// Přesune soubor do Rescue Center místo trvalého smazání
        /// </summary>
        public RescueEntry MoveToRescue(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Soubor nenalezen: {filePath}");

            var fileInfo = new FileInfo(filePath);
            string uniqueName = $"{Guid.NewGuid():N}_{fileInfo.Name}";
            string destPath = Path.Combine(_rescueFolder, uniqueName);

            File.Move(filePath, destPath);

            var entry = new RescueEntry
            {
                OriginalPath = filePath,
                RescuePath = destPath,
                FileName = fileInfo.Name,
                SizeBytes = fileInfo.Length,
                MovedDate = DateTime.Now,
                ExpiresDate = DateTime.Now.AddDays(30)
            };

            _entries.Add(entry);
            SaveManifest();
            return entry;
        }

        /// <summary>
        /// Obnoví soubor z Rescue Center na původní místo
        /// </summary>
        public bool RestoreFile(RescueEntry entry)
        {
            try
            {
                if (!File.Exists(entry.RescuePath)) return false;

                // Zajistit, že cílová složka existuje
                string? dir = Path.GetDirectoryName(entry.OriginalPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.Move(entry.RescuePath, entry.OriginalPath, overwrite: false);
                _entries.Remove(entry);
                SaveManifest();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při obnově souboru: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Trvale smaže soubor z Rescue Center
        /// </summary>
        public bool PermanentlyDelete(RescueEntry entry)
        {
            try
            {
                if (File.Exists(entry.RescuePath))
                    File.Delete(entry.RescuePath);

                _entries.Remove(entry);
                SaveManifest();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při mazání z Rescue Center: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vyčistí soubory starší než 30 dní
        /// </summary>
        public int CleanupExpired()
        {
            int cleaned = 0;
            var expired = _entries.FindAll(e => e.ExpiresDate <= DateTime.Now);

            foreach (var entry in expired)
            {
                if (PermanentlyDelete(entry))
                    cleaned++;
            }

            return cleaned;
        }

        /// <summary>
        /// Vrátí všechny záznamy v Rescue Center
        /// </summary>
        public List<RescueEntry> GetAllEntries() => new(_entries);

        /// <summary>
        /// Vypočítá celkovou velikost Rescue Center
        /// </summary>
        public long GetTotalSize()
        {
            long total = 0;
            foreach (var entry in _entries)
            {
                if (File.Exists(entry.RescuePath))
                    total += new FileInfo(entry.RescuePath).Length;
            }
            return total;
        }

        private void LoadManifest()
        {
            try
            {
                if (File.Exists(_manifestPath))
                {
                    string json = File.ReadAllText(_manifestPath);
                    _entries = JsonSerializer.Deserialize<List<RescueEntry>>(json) ?? new();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při načítání manifestu: {ex.Message}");
                _entries = new();
            }
        }

        private void SaveManifest()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_entries, options);
                File.WriteAllText(_manifestPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při ukládání manifestu: {ex.Message}");
            }
        }
    }
}
