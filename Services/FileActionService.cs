using Microsoft.VisualBasic.FileIO; // Vyžaduje referenci na Microsoft.VisualBasic
using System;

namespace DuplicateFinder.Services
{
    public static class FileActionService
    {
        // FR-05: Přesunout do koše
        public static void SendToRecycleBin(string filePath)
        {
            try
            {
                FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            catch (Exception ex)
            {
                // Zalogovat chybu
                throw new Exception($"Nepodařilo se smazat soubor: {ex.Message}");
            }
        }
    }
}