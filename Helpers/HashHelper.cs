using System;
using System.IO;
using System.Security.Cryptography;

namespace DuplicateFinder.Helpers
{
    public static class HashHelper
    {
        private const int PreCheckSize = 4096; // 4 KB pre-check

        // Rychlý pre-check (první a poslední 4KB)
        public static string CalculatePreCheckHash(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var md5 = MD5.Create();
                
                if (fs.Length <= PreCheckSize * 2)
                {
                    // Pokud je soubor malý, spočítáme rovnou celý hash
                    return BitConverter.ToString(md5.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
                }

                byte[] buffer = new byte[PreCheckSize * 2];
                fs.ReadExactly(buffer, 0, PreCheckSize);
                fs.Seek(-PreCheckSize, SeekOrigin.End);
                fs.ReadExactly(buffer, PreCheckSize, PreCheckSize);

                return BitConverter.ToString(md5.ComputeHash(buffer)).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                // Zalogovat chybu (přístup odepřen atd.)
                Console.WriteLine($"Chyba pre-check hashe {filePath}: {ex.Message}");
                return string.Empty;
            }
        }

        // Plný výpočet hashe (MD5 pro MVP)
        public static string CalculateFullHash(string filePath)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba plného hashe {filePath}: {ex.Message}");
                return string.Empty;
            }
        }
    }
}