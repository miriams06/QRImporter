using Importer.Core.Config;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Importer.Api.Storage;

/// <summary>
/// Abstrai o upload/download para MinIO (S3).
/// Mantém o controller limpo e facilita trocar storage no futuro.
/// </summary>
public interface IStorageService
{
    Task EnsureBucketAsync(CancellationToken ct = default);

    /// <summary>Faz upload e devolve o objectKey.</summary>
    Task<string> PutAsync(
        Stream content,
        string contentType,
        string objectKey,
        CancellationToken ct = default);

    Task<Stream> GetAsync(string objectKey, CancellationToken ct = default);
}

public sealed class S3StorageService : IStorageService
{
    private readonly IMinioClient _minio;
    private readonly ApiConfig _cfg;

    public S3StorageService(IMinioClient minio, IOptions<ApiConfig> cfg)
    {
        _minio = minio;
        _cfg = cfg.Value;
    }

    public async Task EnsureBucketAsync(CancellationToken ct = default)
    {
        var bucket = _cfg.StorageBucket;

        var exists = await _minio.BucketExistsAsync(new BucketExistsArgs()
            .WithBucket(bucket), ct);

        if (!exists)
        {
            await _minio.MakeBucketAsync(new MakeBucketArgs()
                .WithBucket(bucket), ct);
        }
    }

    public async Task<string> PutAsync(Stream content, string contentType, string objectKey, CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);

        // Atenção: precisa de length. Se o stream não suportar Length, copia para MemoryStream.
        long length = content.CanSeek ? content.Length : -1;
        if (length < 0)
        {
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            ms.Position = 0;
            return await PutAsync(ms, contentType, objectKey, ct);
        }

        var put = new PutObjectArgs()
            .WithBucket(_cfg.StorageBucket)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(length)
            .WithContentType(contentType);

        await _minio.PutObjectAsync(put, ct);
        return objectKey;
    }

    public async Task<Stream> GetAsync(string objectKey, CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);

        var ms = new MemoryStream();

        var get = new GetObjectArgs()
            .WithBucket(_cfg.StorageBucket)
            .WithObject(objectKey)
            .WithCallbackStream(stream =>
            {
                stream.CopyTo(ms);
            });

        await _minio.GetObjectAsync(get, ct);
        ms.Position = 0;
        return ms;
    }
}