using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GZCTF.Storage;
using Xunit;

namespace GZCTF.Test.UnitTests.Storage;

public class LocalBlobStorageTests
{
    [Fact]
    public async Task WriteAsync_AllowsConcurrentOverwriteOfSamePath()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gzctf-local-blob-{Guid.NewGuid():N}");
        var storage = new LocalBlobStorage(root);
        await storage.EnsureInitializedAsync();

        const string path = "uploads/ae/f6/aef68645bf32626a51b9420b91a3ad2f1af01cfa8aebb929d2144c1f3944b624";
        byte[] content = [137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3, 4];

        try
        {
            var writes = Enumerable.Range(0, 8)
                .Select(_ => storage.WriteAsync(path, new MemoryStream(content)))
                .ToArray();

            await Task.WhenAll(writes);

            await using var stored = await storage.OpenReadAsync(path);
            using var actual = new MemoryStream();
            await stored.CopyToAsync(actual);

            Assert.Equal(content, actual.ToArray());
            Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(Path.Combine(root, path))!, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
