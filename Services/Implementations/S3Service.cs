using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

public class S3Service : IS3Service
{
    private readonly string bucketName;
    private readonly IAmazonS3 s3Client;
    private readonly string region;

    public S3Service(IConfiguration configuration)
    {
        var awsOptions = configuration.GetSection("AWS");
        bucketName = awsOptions["BucketName"]!;
        region = awsOptions["Region"]!;

        s3Client = new AmazonS3Client(
            awsOptions["AccessKey"],
            awsOptions["SecretKey"],
            RegionEndpoint.GetBySystemName(region)
        );
    }

    public async Task<string> UploadFileAsync(IFormFile file, string baseName)
    {
        var fileExtension = Path.GetExtension(file.FileName);
        var safeName = Path.GetFileNameWithoutExtension(baseName);
        var finalFileName = $"{safeName}-{Guid.NewGuid()}{fileExtension}";

        using var stream = file.OpenReadStream();
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = stream,
            Key = finalFileName,
            BucketName = bucketName,
            ContentType = file.ContentType
        };

        var transferUtility = new TransferUtility(s3Client);
        await transferUtility.UploadAsync(uploadRequest);

        return $"https://{bucketName}.s3.{region}.amazonaws.com/{finalFileName}";
    }

    public async Task<bool> DeleteFileAsync(string fileKey)
    {
        var request = new Amazon.S3.Model.DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = fileKey
        };
        var response = await s3Client.DeleteObjectAsync(request);
        return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
    }
}