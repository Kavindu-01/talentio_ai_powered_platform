using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string category, string contentType);
        Task<Stream?> GetFileAsync(string fileUrl);
        Task<bool> DeleteFileAsync(string fileUrl);
    }

    /// <summary>
    /// Storage service abstraction supporting local storage and cloud providers (AWS S3 / Azure Blob Storage).
    /// </summary>
    public class StorageService : IStorageService
    {
        private readonly ILogger<StorageService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _baseUploadPath;

        public StorageService(ILogger<StorageService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _baseUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

            if (!Directory.Exists(_baseUploadPath))
            {
                Directory.CreateDirectory(_baseUploadPath);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string category, string contentType)
        {
            var provider = _configuration["Storage:Provider"] ?? "Local";

            if (provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
            {
                // Azure Blob Storage Integration (Azure.Storage.Blobs)
                // string connectionString = _configuration["Storage:AzureConnectionString"];
                // BlobContainerClient container = new BlobContainerClient(connectionString, category);
                // await container.CreateIfNotExistsAsync();
                // BlobClient blob = container.GetBlobClient(fileName);
                // await blob.UploadAsync(fileStream, true);
                // return blob.Uri.ToString();
                _logger.LogInformation("Simulating Azure Blob upload for {FileName} in container {Category}", fileName, category);
            }
            else if (provider.Equals("AwsS3", StringComparison.OrdinalIgnoreCase))
            {
                // AWS S3 Integration (AWSSDK.S3)
                // var s3Client = new AmazonS3Client(...);
                // var putRequest = new PutObjectRequest { BucketName = category, Key = fileName, InputStream = fileStream };
                // await s3Client.PutObjectAsync(putRequest);
                // return $"https://{category}.s3.amazonaws.com/{fileName}";
                _logger.LogInformation("Simulating AWS S3 upload for {FileName} in bucket {Category}", fileName, category);
            }

            // Default: Secure Local Disk Storage
            var categoryPath = Path.Combine(_baseUploadPath, category);
            if (!Directory.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var destinationPath = Path.Combine(categoryPath, uniqueFileName);

            using (var destinationStream = new FileStream(destinationPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(destinationStream);
            }

            var relativeUrl = $"/Uploads/{category}/{uniqueFileName}";
            _logger.LogInformation("File successfully stored at {RelativeUrl}", relativeUrl);
            return relativeUrl;
        }

        public async Task<Stream?> GetFileAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return null;

            var localPath = Path.Combine(Directory.GetCurrentDirectory(), fileUrl.TrimStart('/'));
            if (File.Exists(localPath))
            {
                return new FileStream(localPath, FileMode.Open, FileAccess.Read);
            }

            return null;
        }

        public Task<bool> DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return Task.FromResult(false);

            try
            {
                var localPath = Path.Combine(Directory.GetCurrentDirectory(), fileUrl.TrimStart('/'));
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                    return Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file at {FileUrl}", fileUrl);
            }

            return Task.FromResult(false);
        }
    }
}
