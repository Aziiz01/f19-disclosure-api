using DisclosureEngine.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace DisclosureEngine.Infrastructure.Storage;

/// <summary>
/// MinIO-backed <see cref="IFileStorage"/>. Production swap to Azure Blob Storage
/// is a one-class change inside <c>DependencyInjection.cs</c> — the contract is
/// stable. See <c>docs/DECISIONS.md</c> §13.
/// </summary>
public sealed class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;
    private readonly ILogger<MinioFileStorage> _logger;
    private int _bucketEnsured;

    public MinioFileStorage(IConfiguration configuration, ILogger<MinioFileStorage> logger)
    {
        var endpoint  = configuration["Minio:Endpoint"]
            ?? throw new InvalidOperationException("Minio:Endpoint not configured.");
        var accessKey = configuration["Minio:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["Minio:SecretKey"] ?? "minioadmin";
        _bucketName   = configuration["Minio:BucketName"] ?? "disclosure-engine-attachments";
        _logger       = logger;

        _client = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("fileName is required.", nameof(fileName));

        await EnsureBucketAsync(ct);

        var storageKey = $"{Guid.NewGuid():N}/{fileName}";

        var args = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(storageKey)
            .WithStreamData(content)
            .WithObjectSize(content.CanSeek ? content.Length : -1)
            .WithContentType(contentType);

        await _client.PutObjectAsync(args, ct);
        _logger.LogInformation("MinIO upload OK: {Key} ({Bytes} bytes)", storageKey, content.CanSeek ? content.Length : 0);
        return storageKey;
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken ct)
    {
        await EnsureBucketAsync(ct);

        var buffer = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(storageKey)
            .WithCallbackStream(async (stream, innerCt) =>
            {
                await stream.CopyToAsync(buffer, innerCt);
            });

        try
        {
            await _client.GetObjectAsync(args, ct);
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            throw new KeyNotFoundException($"No object for storage key '{storageKey}'.");
        }

        buffer.Position = 0;
        return buffer;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        await EnsureBucketAsync(ct);

        var args = new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(storageKey);

        try
        {
            await _client.RemoveObjectAsync(args, ct);
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            // Idempotent: treating "already gone" as success matches IFileStorage contract.
        }
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref _bucketEnsured, 1, 0) == 1) return;

        var exists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName), ct);
        if (!exists)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName), ct);
            _logger.LogInformation("Created MinIO bucket '{Bucket}'.", _bucketName);
        }
    }
}
