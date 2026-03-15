using Minio;
using Minio.DataModel.Args;

namespace Application.Services;

public class MinioService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public MinioService(IMinioClient minioClient, IConfiguration configuration)
    {
        _minioClient = minioClient;
        _bucketName = configuration["MINIO_BUCKET"] ?? "images";
    }

    public async Task<string> UploadProductImageAsync(IFormFile file, Guid productId)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        await EnsureBucketExistsAsync();

        var fileName = $"{productId}/{Guid.NewGuid()}_{file.FileName}";

        using (var stream = file.OpenReadStream())
        {
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithStreamData(stream)
                .WithObjectSize(file.Length)
                .WithContentType(file.ContentType ?? "application/octet-stream");

            await _minioClient.PutObjectAsync(putObjectArgs);
        }

        return fileName;
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(_bucketName);
            
            var bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

            if (!bucketExists)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(_bucketName);
                
                await _minioClient.MakeBucketAsync(makeBucketArgs);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to ensure bucket exists: {ex.Message}", ex);
        }
    }
}
