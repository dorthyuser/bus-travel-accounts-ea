using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CreateAccountLambda
{
    public class Function
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _s3Bucket;
        private readonly string _awsAccessKey;
        private readonly string _awsSecretKey;
        private readonly string _awsRegion;
        private readonly string _appName;

        public Function()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            _configuration = builder.Build();

            var host = _configuration["https.requester.sa.accounts.host"] ?? "";
            var port = _configuration["https.requester.sa.accounts.port"] ?? "";
            var basePath = _configuration["https.requester.sa.accounts.basepath"] ?? "";
            var scheme = "https";
            var baseUri = scheme + "://" + host + (string.IsNullOrEmpty(port) ? "" : ":" + port) + basePath;

            _httpClient = new HttpClient { BaseAddress = new Uri(baseUri) };

            _s3Bucket = _configuration["amazon.s3.bucket"] ?? "";
            _awsAccessKey = _configuration["secure::amazon.s3.accessKey"] ?? "";
            _awsSecretKey = _configuration["secure::amazon.s3.secretKey"] ?? "";
            _awsRegion = _configuration["amazon.s3.region"] ?? "us-east-1";
            _appName = _configuration["json.logger.application.name"] ?? "bus-travel-accounts-ea";
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var correlationId = request.Headers != null && request.Headers.ContainsKey("X_CORRELATION_ID") ? request.Headers["X_CORRELATION_ID"] : Guid.NewGuid().ToString();
            var customerCorrelationId = request.Headers != null && request.Headers.ContainsKey("CUSTOMER_CORRELATION_ID") ? request.Headers["CUSTOMER_CORRELATION_ID"] : "CUSTOMER_CORRELATION_ID_NOT_FOUND";

            context.Logger.LogLine($"START - CreateAccount - X_CORRELATION_ID:{correlationId}");

            try
            {
                // Body must be forwarded to backend
                var body = request.Body ?? string.Empty;

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/accounts")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };

                httpRequest.Headers.Add("CUSTOMER_CORRELATION_ID", customerCorrelationId);
                httpRequest.Headers.Add("X_CORRELATION_ID", correlationId);

                var clientSecret = _configuration["secure::https.requester.sa.accounts.client_secret"] ?? "";
                var clientId = _configuration["https.requester.sa.accounts.client_id"] ?? "";
                if (!string.IsNullOrEmpty(clientSecret)) httpRequest.Headers.Add("client_secret", clientSecret);
                if (!string.IsNullOrEmpty(clientId)) httpRequest.Headers.Add("client_id", clientId);

                var response = await _httpClient.SendAsync(httpRequest);
                var responseBody = await response.Content.ReadAsStringAsync();

                context.Logger.LogLine($"After Request - httpStatus: {(int)response.StatusCode} - X_CORRELATION_ID:{correlationId}");

                // Mule expects 201 for created path handling; mirror status
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Body = responseBody,
                    Headers = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" },
                        { "X_CORRELATION_ID", correlationId }
                    }
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"ERROR - {ex}");
                var errorPayload = new
                {
                    error = new
                    {
                        errorCode = 500,
                        errorDateTime = DateTime.UtcNow.ToString("o"),
                        errorMessage = "INTERNAL SERVER ERROR",
                        errorDescription = ex.Message
                    }
                };

                var errorJson = JsonSerializer.Serialize(errorPayload);

                try
                {
                    var creds = new BasicAWSCredentials(_awsAccessKey, _awsSecretKey);
                    using var s3 = new AmazonS3Client(creds, Amazon.RegionEndpoint.GetBySystemName(_awsRegion));
                    var key = _appName + "-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-error.json";
                    var putReq = new PutObjectRequest { BucketName = _s3Bucket, Key = key, ContentBody = errorJson };
                    await s3.PutObjectAsync(putReq);
                }
                catch (Exception s3ex)
                {
                    context.Logger.LogLine($"Failed to upload to S3: {s3ex}");
                }

                return new APIGatewayProxyResponse { StatusCode = 500, Body = errorJson, Headers = new System.Collections.Generic.Dictionary<string, string> { { "Content-Type", "application/json" } } };
            }
        }
    }
}
