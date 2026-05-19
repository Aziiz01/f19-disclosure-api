namespace DisclosureEngine.Application.Common.Interfaces;

/// <summary>
/// Opaque blob store. The returned storage key is implementation-specific (file path, S3 key, etc.)
/// — persist it as-is and pass it back to retrieve or delete.
/// </summary>
public interface IFileStorage
{
    /// <summary>Uploads the stream and returns the storage key to persist.</summary>
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct);

    /// <summary>Returns a stream for the given key. Throws KeyNotFoundException if missing.</summary>
    Task<Stream> DownloadAsync(string storageKey, CancellationToken ct);

    /// <summary>Deletes the blob. No-op if the key doesn't exist.</summary>
    Task DeleteAsync(string storageKey, CancellationToken ct);
}
