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

    public S3Service(IConfiguration configuration)
    {
        var awsOptions = configuration.GetSection("AWS");
        bucketName = awsOptions["BucketName"]!;

        s3Client = new AmazonS3Client(
            awsOptions["AccessKey"],
            awsOptions["SecretKey"],
            RegionEndpoint.GetBySystemName(awsOptions["Region"])
        );
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        using var stream = file.OpenReadStream();

        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = stream,
            Key = fileName,
            BucketName = bucketName,
            ContentType = file.ContentType
        };

        var transferUtility = new TransferUtility(s3Client);
        await transferUtility.UploadAsync(uploadRequest);

        return $"https://{bucketName}.s3.amazonaws.com/{fileName}";
    }
}
