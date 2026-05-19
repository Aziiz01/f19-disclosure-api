using System.Collections.Concurrent;
using DisclosureEngine.Application.Common.Interfaces;

namespace DisclosureEngine.Infrastructure.Storage;

public sealed class InMemoryFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("fileName is required.", nameof(fileName));

        await using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var storageKey = $"{Guid.NewGuid():N}/{fileName}";
        _store[storageKey] = bytes;
        return storageKey;
    }

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken ct)
    {
        if (!_store.TryGetValue(storageKey, out var bytes))
            throw new KeyNotFoundException($"No file found for storage key '{storageKey}'.");

        return Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        _store.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }
}
