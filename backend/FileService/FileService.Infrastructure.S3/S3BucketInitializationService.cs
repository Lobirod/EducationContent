using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using FileService.Core.FileStorage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public class S3BucketInitializationService : BackgroundService
{
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3BucketInitializationService> _logger;

    public S3BucketInitializationService(
        IOptions<FileStorageOptions> s3Options,
        IAmazonS3 s3Client,
        ILogger<S3BucketInitializationService> logger)
    {
        _fileStorageOptions = s3Options.Value;
        _s3Client = s3Client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        
        try
        {
            _logger.LogInformation("S3 bucket initialization service started");

            if (_fileStorageOptions.RequiredBuckets.Count == 0)
            {
                _logger.LogInformation("S3 bucket initialization service required buckets");
                throw new ArgumentException("RequiredBuckets is required");
            }
            
            _logger.LogInformation(
                "Starting S3 bucket initialization. Required buckets: {Buckets}",
                string.Join(",", _fileStorageOptions.RequiredBuckets));
            
            var tasks = _fileStorageOptions.RequiredBuckets
                .Select(bucketName => InitializeBucketAsync(bucketName, stoppingToken))
                .ToArray();
            
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("S3 bucket initialization service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error during S3 bucket initialization");
            throw;
        }
    }

    private async Task InitializeBucketAsync(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            bool bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
            if (bucketExists)
            {
                _logger.LogInformation("Bucket {Bucket} already exists", bucketName);
                return;
            }

            _logger.LogInformation("Creating bucket '{BucketName}'", bucketName);

            var putBucketRequest = new PutBucketRequest
            {
                BucketName = bucketName,
            };

            await _s3Client.PutBucketAsync(putBucketRequest, cancellationToken);

            string policy = $$"""
                              {
                                  "Version": "2012-10-17",
                                  "Statement": [
                                      {
                                      "Effect": "Allow",
                                      "Principal": {
                                          "AWS": ["*"]
                                      },
                                      "Action": ["s3:GetObject"],
                                      "Resource": ["arn:aws:s3:::{{bucketName}}/*"]
                                      }
                                  ]
                              }
                              """;

            var putPolicyRequest = new PutBucketPolicyRequest
            {
                BucketName = bucketName,
                Policy = policy,
            };

            await _s3Client.PutBucketPolicyAsync(putPolicyRequest, cancellationToken);

            _logger.LogInformation("Bucket '{BucketName}' created successfully", bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize bucket '{BucketName}'", bucketName);
            throw;
        }
    }

}