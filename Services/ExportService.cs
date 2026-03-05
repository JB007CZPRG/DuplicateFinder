using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DuplicateFinder.Models;
using DuplicateFinder.Helpers;

namespace DuplicateFinder.Services
{
    /// <summary>
    /// Export výsledků skenování do CSV nebo HTML (FR-07)
    /// </summary>
    public static class ExportService
    {
        /// <summary>
        /// Exportuje výsledky do CSV souboru
        /// </summary>
        public static void ExportToCsv(IEnumerable<FileEntry> results, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Název;Cesta;Velikost;Datum úpravy;Hash;Stav;Složka");

            foreach (var file in results)
            {
                string statusText = GetStatusText(file.Status);
                sb.AppendLine($"\"{file.FileName}\";\"{file.FullPath}\";\"{FileSizeHelper.FormatSize(file.SizeBytes)}\";\"{file.LastModified:yyyy-MM-dd HH:mm}\";\"{file.Hash}\";\"{statusText}\";\"{file.FolderSource}\"");
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Exportuje výsledky do HTML reportu
        /// </summary>
        public static void ExportToHtml(IEnumerable<FileEntry> results, string outputPath, string folderA = "", string folderB = "")
        {
            var fileList = results.ToList();
            int duplicates = fileList.Count(f => f.Status == DuplicateStatus.ExactDuplicate);
            int probable = fileList.Count(f => f.Status == DuplicateStatus.ProbableDuplicate);
            int unique = fileList.Count(f => f.Status == DuplicateStatus.Unique);
            long savedSpace = fileList
                .Where(f => f.Status == DuplicateStatus.ExactDuplicate)
                .GroupBy(f => f.Hash)
                .Sum(g => g.Skip(1).Sum(f => f.SizeBytes));

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"cs\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <title>DuplicateFinder – Report</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: 'Segoe UI', sans-serif; margin: 20px; background: #f5f5f5; }");
            sb.AppendLine("    h1 { color: #0078D4; }");
            sb.AppendLine("    .summary { display: flex; gap: 20px; margin: 20px 0; }");
            sb.AppendLine("    .card { background: white; border-radius: 8px; padding: 15px 25px; box-shadow: 0 2px 4px rgba(0,0,0,.1); text-align: center; }");
            sb.AppendLine("    .card h2 { margin: 0; font-size: 2em; }");
            sb.AppendLine("    .card p { margin: 5px 0 0; color: #666; }");
            sb.AppendLine("    .card.red h2 { color: #c42b1c; }");
            sb.AppendLine("    .card.yellow h2 { color: #9d8500; }");
            sb.AppendLine("    .card.green h2 { color: #0f7b0f; }");
            sb.AppendLine("    .card.blue h2 { color: #0078D4; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,.1); }");
            sb.AppendLine("    th { background: #0078D4; color: white; padding: 10px 12px; text-align: left; }");
            sb.AppendLine("    td { padding: 8px 12px; border-bottom: 1px solid #eee; }");
            sb.AppendLine("    tr:hover td { background: #f0f6ff; }");
            sb.AppendLine("    .status-exact { background: #FDE7E9; color: #c42b1c; padding: 2px 8px; border-radius: 4px; }");
            sb.AppendLine("    .status-probable { background: #FFF4CE; color: #9d8500; padding: 2px 8px; border-radius: 4px; }");
            sb.AppendLine("    .status-unique { background: #DFF6DD; color: #0f7b0f; padding: 2px 8px; border-radius: 4px; }");
            sb.AppendLine("    footer { margin-top: 30px; color: #999; font-size: 0.85em; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine($"  <h1>🔍 DuplicateFinder – Report</h1>");
            sb.AppendLine($"  <p>Vygenerováno: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

            if (!string.IsNullOrEmpty(folderA) || !string.IsNullOrEmpty(folderB))
            {
                sb.AppendLine($"  <p><strong>Složka A:</strong> {folderA}<br/><strong>Složka B:</strong> {folderB}</p>");
            }

            sb.AppendLine("  <div class=\"summary\">");
            sb.AppendLine($"    <div class=\"card red\"><h2>{duplicates}</h2><p>Přesné duplicity</p></div>");
            sb.AppendLine($"    <div class=\"card yellow\"><h2>{probable}</h2><p>Pravděpodobné</p></div>");
            sb.AppendLine($"    <div class=\"card green\"><h2>{unique}</h2><p>Unikátní</p></div>");
            sb.AppendLine($"    <div class=\"card blue\"><h2>{FileSizeHelper.FormatSize(savedSpace)}</h2><p>Možné úspory</p></div>");
            sb.AppendLine("  </div>");

            sb.AppendLine("  <table>");
            sb.AppendLine("    <thead><tr><th>#</th><th>Název</th><th>Velikost</th><th>Datum</th><th>Hash</th><th>Stav</th><th>Složka</th></tr></thead>");
            sb.AppendLine("    <tbody>");

            int index = 1;
            foreach (var file in fileList)
            {
                string statusClass = file.Status switch
                {
                    DuplicateStatus.ExactDuplicate => "status-exact",
                    DuplicateStatus.ProbableDuplicate => "status-probable",
                    _ => "status-unique"
                };
                string statusText = GetStatusText(file.Status);

                sb.AppendLine($"    <tr>");
                sb.AppendLine($"      <td>{index++}</td>");
                sb.AppendLine($"      <td>{System.Net.WebUtility.HtmlEncode(file.FileName)}</td>");
                sb.AppendLine($"      <td>{FileSizeHelper.FormatSize(file.SizeBytes)}</td>");
                sb.AppendLine($"      <td>{file.LastModified:yyyy-MM-dd HH:mm}</td>");
                sb.AppendLine($"      <td style=\"font-family:monospace;font-size:0.85em\">{(file.Hash.Length > 12 ? file.Hash[..12] + "…" : file.Hash)}</td>");
                sb.AppendLine($"      <td><span class=\"{statusClass}\">{statusText}</span></td>");
                sb.AppendLine($"      <td>{file.FolderSource}</td>");
                sb.AppendLine($"    </tr>");
            }

            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");
            sb.AppendLine("  <footer>DuplicateFinder © 2026</footer>");
            sb.AppendLine("</body></html>");

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private static string GetStatusText(DuplicateStatus status) => status switch
        {
            DuplicateStatus.ExactDuplicate => "🔴 Duplicita",
            DuplicateStatus.ProbableDuplicate => "🟡 Podobný",
            DuplicateStatus.Unique => "🟢 Unikátní",
            _ => "Neznámý"
        };
    }
}
