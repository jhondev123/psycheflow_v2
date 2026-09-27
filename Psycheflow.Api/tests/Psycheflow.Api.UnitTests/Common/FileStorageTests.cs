using Microsoft.Extensions.Options;
using Psycheflow.Api.Common.Storage;

namespace Psycheflow.Api.UnitTests.Common;

public sealed class FileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"psycheflow-storage-{Guid.NewGuid():N}");
    private readonly LocalFileStorage _storage;

    public FileStorageTests() => _storage = new LocalFileStorage(Options.Create(new StorageOptions { Path = _root }));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveThenOpen_ReturnsTheSameBytes()
    {
        byte[] content = [1, 2, 3, 4, 5];

        await _storage.SaveAsync("empresa/arquivo", new MemoryStream(content), TestContext.Current.CancellationToken);
        await using Stream stream = await _storage.OpenReadAsync("empresa/arquivo", TestContext.Current.CancellationToken);
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, TestContext.Current.CancellationToken);

        copy.ToArray().ShouldBe(content);
    }

    [Fact]
    public async Task Delete_RemovesTheFile()
    {
        await _storage.SaveAsync("a/b", new MemoryStream([9]), TestContext.Current.CancellationToken);

        await _storage.DeleteAsync("a/b", TestContext.Current.CancellationToken);

        await Should.ThrowAsync<FileNotFoundException>(() => _storage.OpenReadAsync("a/b", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("../fora")]
    [InlineData("a/../../fora")]
    [InlineData("/absoluto")]
    public async Task Keys_CannotEscapeTheStorageRoot(string key) =>
        await Should.ThrowAsync<ArgumentException>(() => _storage.SaveAsync(key, new MemoryStream([1]), TestContext.Current.CancellationToken));
}
