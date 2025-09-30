using Minio;
using Minio.DataModel.Args;

namespace SmartUnderwrite.Api.Services;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IMinioClient minioClient, IConfiguration configuration, ILogger<MinioStorageService> logger)
    {
        _minioClient = minioClient;
        _bucketName = configuration.GetValue<string>("Storage:BucketName") ?? "smartunderwrite-documents";
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? folder = null)
    {
        try
        {
            // Ensure bucket exists
            await EnsureBucketExistsAsync();

            // Generate unique file path
            var timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var sanitizedFileName = SanitizeFileName(fileName);
            var filePath = folder != null 
                ? $"{folder}/{timestamp}/{uniqueId}_{sanitizedFileName}"
                : $"{timestamp}/{uniqueId}_{sanitizedFileName}";

            _logger.LogInformation("Uploading file to MinIO: {FilePath}", filePath);

            // Upload file
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(filePath)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            _logger.LogInformation("Successfully uploaded file: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName}", fileName);
            throw new StorageException($"Failed to upload file: {ex.Message}", ex);
        }
    }

    public async Task<Stream> GetFileAsync(string filePath)
    {
        try
        {
            _logger.LogDebug("Retrieving file from MinIO: {FilePath}", filePath);

            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(filePath)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs);
            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve file {FilePath}", filePath);
            throw new StorageException($"Failed to retrieve file: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Deleting file from MinIO: {FilePath}", filePath);

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(filePath);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);

            _logger.LogInformation("Successfully deleted file: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FilePath}", filePath);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(filePath);

            await _minioClient.StatObjectAsync(statObjectArgs);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucketName);
            var bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

            if (!bucketExists)
            {
                _logger.LogInformation("Creating MinIO bucket: {BucketName}", _bucketName);
                var makeBucketArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure bucket exists: {BucketName}", _bucketName);
            throw new StorageException($"Failed to ensure bucket exists: {ex.Message}", ex);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove or replace invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = fileName;
        
        foreach (var invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar, '_');
        }

        // Limit length and ensure it's not empty
        sanitized = sanitized.Length > 100 ? sanitized[..100] : sanitized;
        return string.IsNullOrWhiteSpace(sanitized) ? "document" : sanitized;
    }
}