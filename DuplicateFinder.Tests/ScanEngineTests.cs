using Xunit;
using DuplicateFinder.Services;
using DuplicateFinder.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DuplicateFinder.Tests
{
    public class ScanEngineTests : IDisposable
    {
        private readonly string _tempPath;
        private readonly string _dirA;
        private readonly string _dirB;
        private readonly ScanEngine _engine;

        public ScanEngineTests()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _dirA = Path.Combine(_tempPath, "FolderA");
            _dirB = Path.Combine(_tempPath, "FolderB");
            
            Directory.CreateDirectory(_dirA);
            Directory.CreateDirectory(_dirB);
            
            _engine = new ScanEngine();
        }

        [Fact]
        public async Task ScanFoldersAsync_DetectsExactDuplicate()
        {
            // Arrange
            string content = "Unikatni data souboru";
            File.WriteAllText(Path.Combine(_dirA, "file1.txt"), content);
            File.WriteAllText(Path.Combine(_dirB, "file2.txt"), content); // Duplikát v B

            // Act
            var progress = new Progress<int>();
            var results = await _engine.ScanFoldersAsync(_dirA, _dirB, progress, CancellationToken.None);

            // Assert
            var duplicates = results.Where(r => r.Status == DuplicateStatus.ExactDuplicate).ToList();
            Assert.Equal(2, duplicates.Count);
        }

        [Fact]
        public async Task ScanFoldersAsync_DetectsUniqueFiles()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_dirA, "uniqueA.txt"), "Data A");
            File.WriteAllText(Path.Combine(_dirB, "uniqueB.txt"), "Data B (jina)");

            // Act
            var progress = new Progress<int>();
            var results = await _engine.ScanFoldersAsync(_dirA, _dirB, progress, CancellationToken.None);

            // Assert
            var unique = results.Where(r => r.Status == DuplicateStatus.Unique).ToList();
            Assert.Equal(2, unique.Count);
        }

        [Fact]
        public async Task ScanFoldersAsync_DetectsProbableDuplicates()
        {
            // Arrange
            // Similar names but different content so hash is different
            File.WriteAllText(Path.Combine(_dirA, "report_2024.txt"), "This is content A");
            File.WriteAllText(Path.Combine(_dirB, "report_2025.txt"), "This is content B");
            
            // To ensure they are the same size for them to be compared in Fuzzy matching
            Assert.Equal(new FileInfo(Path.Combine(_dirA, "report_2024.txt")).Length, new FileInfo(Path.Combine(_dirB, "report_2025.txt")).Length);

            // Act
            var progress = new Progress<int>();
            var results = await _engine.ScanFoldersAsync(_dirA, _dirB, progress, CancellationToken.None);

            // Assert
            var probable = results.Where(r => r.Status == DuplicateStatus.ProbableDuplicate).ToList();
            Assert.Equal(2, probable.Count);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempPath))
                Directory.Delete(_tempPath, true);
        }
    }
}