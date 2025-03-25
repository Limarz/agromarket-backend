using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

public interface IYandexCloudStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string fileName);
}

public class YandexCloudStorageService : IYandexCloudStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public YandexCloudStorageService(IConfiguration configuration)
    {
        var accessKey = configuration["YCAJEqN4afaztfw2_t_erAs5"];
        var secretKey = configuration["YYCN103SM1uxOXkPxVE8lnUGNFQWKI5-j6VUYcOMc"];
        _bucketName = configuration["agromarket-bucket"];
        var endpoint = configuration["https://storage.yandexcloud.net"];

        var s3Config = new AmazonS3Config
        {
            ServiceURL = endpoint
        };
        _s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string fileName)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(request);
            return $"https://storage.yandexcloud.net/{_bucketName}/{fileName}";
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка загрузки файла в Yandex Cloud: {ex.Message}");
        }
    }
}