using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace TimeTracker.API.Services
{
    public interface IS3Service
    {
        Task<(string originalUrl, string thumbnailUrl)> UploadScreenshotAsync(IFormFile file, string userId);
    }

    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly ILogger<S3Service> _logger;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3Service> logger)
        {
            _s3Client = s3Client;
            _bucketName = configuration["AWS:S3BucketName"] ?? throw new ArgumentNullException("S3BucketName");
            _logger = logger;
        }

        public async Task<(string originalUrl, string thumbnailUrl)> UploadScreenshotAsync(IFormFile file, string userId)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
                var originalKey = $"screenshots/{userId}/{timestamp}_original.jpg";
                var thumbnailKey = $"screenshots/{userId}/{timestamp}_thumbnail.jpg";

                // Upload original image
                using var originalStream = file.OpenReadStream();
                var originalRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = originalKey,
                    InputStream = originalStream,
                    ContentType = "image/jpeg",
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                await _s3Client.PutObjectAsync(originalRequest);
                var originalUrl = $"https://{_bucketName}.s3.amazonaws.com/{originalKey}";

                // Create and upload thumbnail
                using var thumbnailStream = await CreateThumbnailAsync(file);
                var thumbnailRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = thumbnailKey,
                    InputStream = thumbnailStream,
                    ContentType = "image/jpeg",
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                await _s3Client.PutObjectAsync(thumbnailRequest);
                var thumbnailUrl = $"https://{_bucketName}.s3.amazonaws.com/{thumbnailKey}";

                _logger.LogInformation("Successfully uploaded screenshot for user {UserId}", userId);
                return (originalUrl, thumbnailUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload screenshot for user {UserId}", userId);
                throw;
            }
        }

        private async Task<MemoryStream> CreateThumbnailAsync(IFormFile file)
        {
            using var image = await Image.LoadAsync(file.OpenReadStream());
            
            // Create thumbnail (300x200 max, maintain aspect ratio)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(300, 200),
                Mode = ResizeMode.Max
            }));

            var thumbnailStream = new MemoryStream();
            await image.SaveAsJpegAsync(thumbnailStream);
            thumbnailStream.Position = 0;
            
            return thumbnailStream;
        }
    }
}
