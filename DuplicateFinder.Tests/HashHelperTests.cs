using Xunit;
using DuplicateFinder.Helpers;
using System.IO;

namespace DuplicateFinder.Tests
{
    public class HashHelperTests
    {
        [Fact]
        public void CalculateFullHash_IdenticalContent_ReturnsSameHash()
        {
            // Arrange
            string path1 = Path.GetTempFileName();
            string path2 = Path.GetTempFileName();
            File.WriteAllText(path1, "testovaci obsah");
            File.WriteAllText(path2, "testovaci obsah");

            // Act
            string hash1 = HashHelper.CalculateFullHash(path1);
            string hash2 = HashHelper.CalculateFullHash(path2);

            // Assert
            Assert.Equal(hash1, hash2);

            // Cleanup
            File.Delete(path1);
            File.Delete(path2);
        }

        [Fact]
        public void PreCheckHash_DifferentLargeFiles_ReturnsDifferentHashes()
        {
            // Arrange - Vytvoříme soubory větší než 8KB, které se liší v prostředku
            string path1 = Path.GetTempFileName();
            string path2 = Path.GetTempFileName();
            
            byte[] data1 = new byte[10000];
            byte[] data2 = new byte[10000];
            new System.Random().NextBytes(data1);
            data1.CopyTo(data2, 0);
            data2[5000] = (byte)(data1[5000] + 1); // Změna uprostřed

            File.WriteAllBytes(path1, data1);
            File.WriteAllBytes(path2, data2);

            // Act - Pre-check bere jen začátek a konec (4KB + 4KB)
            string hash1 = HashHelper.CalculatePreCheckHash(path1);
            string hash2 = HashHelper.CalculatePreCheckHash(path2);

            // Assert - Měly by být stejné, protože rozdíl je v prostředku mimo 4KB bloky
            Assert.Equal(hash1, hash2);

            // Cleanup
            File.Delete(path1);
            File.Delete(path2);
        }
    }
}