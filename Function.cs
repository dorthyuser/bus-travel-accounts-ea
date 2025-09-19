using System;
using System.IO;
using System.Net.Http;
using System.Text;
<<<<<<< HEAD
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
=======
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
>>>>>>> 1ae0df3 (Automated commit on branch UpdateAccountLambda from AI2DEV)

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace UpdateAccountLambda
{
    public class Function
    {
<<<<<<< HEAD
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _s3Bucket;
        private readonly string _awsAccessKey;
        private readonly string _awsSecretKey;
        private readonly string _awsRegion;
        private readonly string _appName;
=======
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly IConfiguration _config;
>>>>>>> 1ae0df3 (Automated commit on branch UpdateAccountLambda from AI2DEV)

        public Function()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
<<<<<<< HEAD
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
=======
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
            _config = builder.Build();
>>>>>>> 1ae0df3 (Automated commit on branch UpdateAccountLambda from AI2DEV)
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
<<<<<<< HEAD
            var correlationId = request.Headers != null && request.Headers.ContainsKey("X_CORRELATION_ID") ? request.Headers["X_CORRELATION_ID"] : Guid.NewGuid().ToString();
            var customerCorrelationId = request.Headers != null && request.Headers.ContainsKey("CUSTOMER_CORRELATION_ID") ? request.Headers["CUSTOMER_CORRELATION_ID"] : "CUSTOMER_CORRELATION_ID_NOT_FOUND";

            context.Logger.LogLine($"START - UpdateAccount - X_CORRELATION_ID:{correlationId}");

            try
            {
                if (request.PathParameters == null || !request.PathParameters.ContainsKey("id"))
                {
                    var bad = new { error = new { errorCode = 400, errorDateTime = DateTime.UtcNow.ToString("o"), errorMessage = "BAD REQUEST", errorDescription = "Missing path parameter id" } };
                    var badJson = JsonSerializer.Serialize(bad);
                    return new APIGatewayProxyResponse { StatusCode = 400, Body = badJson, Headers = new System.Collections.Generic.Dictionary<string, string> { { "Content-Type", "application/json" } } };
                }

                var accountId = request.PathParameters["id"].Trim();
                var body = request.Body ?? string.Empty;

                using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/accounts/{Uri.EscapeDataString(accountId)}")
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
=======
            var correlationId = request?.Headers != null && request.Headers.ContainsKey("X_CORRELATION_ID") ? request.Headers["X_CORRELATION_ID"] : Guid.NewGuid().ToString();
            var customerCorrelationId = request?.Headers != null && request.Headers.ContainsKey("CUSTOMER_CORRELATION_ID") ? request.Headers["CUSTOMER_CORRELATION_ID"] : "CUSTOMER_CORRELATION_ID_NOT_FOUND";

            try
            {
                string accountId = null;
                if (request?.PathParameters != null && request.PathParameters.TryGetValue("id", out var idVal)) accountId = idVal?.Trim();

                context.Logger.LogLine($"START - Request received. X_CORRELATION_ID={correlationId} CUSTOMER_CORRELATION_ID={customerCorrelationId}");
                context.Logger.LogLine($"Before Request - bus-travel-accounts-sa-update-account. AccountId={accountId}");

                var host = _config["https.requester.sa.accounts.host"];
                var port = _config["https.requester.sa.accounts.port"];
                var basePath = _config["https.requester.sa.accounts.basepath"] ?? string.Empty;
                var clientId = _config["https.requester.sa.accounts.client_id"];
                var clientSecret = _config["secure::https.requester.sa.accounts.client_secret"] ?? string.Empty;
                var timeoutMs = 30000;
                if (int.TryParse(_config["https.requester.sa.accounts.response.timeout"], out var t)) timeoutMs = t;

                _httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(host))
                {
                    sb.Append(host);
                    if (!string.IsNullOrEmpty(port)) sb.Append(":").Append(port);
                }
                if (!string.IsNullOrEmpty(basePath))
                {
                    if (!basePath.StartsWith("/")) sb.Append('/');
                    sb.Append(basePath.TrimEnd('/'));
                }
                if (!sb.ToString().EndsWith("/")) sb.Append('/');
                sb.Append("accounts/").Append(Uri.EscapeDataString(accountId ?? string.Empty));

                var url = sb.ToString();
                var payload = request?.Body ?? string.Empty;

                var httpRequest = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Add("CUSTOMER_CORRELATION_ID", customerCorrelationId);
                httpRequest.Headers.Add("X_CORRELATION_ID", correlationId);
                if (!string.IsNullOrEmpty(clientId)) httpRequest.Headers.Add("client_id", clientId);
                if (!string.IsNullOrEmpty(clientSecret)) httpRequest.Headers.Add("client_secret", clientSecret);

                var response = await _httpClient.SendAsync(httpRequest).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                context.Logger.LogLine($"After Request - bus-travel-accounts-sa-update-account. httpStatus={(int)response.StatusCode}");

                if ((int)response.StatusCode == 200)
                {
                    return new APIGatewayProxyResponse { StatusCode = 200, Body = responseBody, Headers = new System.Collections.Generic.Dictionary<string, string> {{"Content-Type","application/json"}} };
                }
                else
                {
                    return new APIGatewayProxyResponse { StatusCode = (int)response.StatusCode, Body = responseBody, Headers = new System.Collections.Generic.Dictionary<string, string> {{"Content-Type","application/json"}} };
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message}");
>>>>>>> 1ae0df3 (Automated commit on branch UpdateAccountLambda from AI2DEV)
                var errorPayload = new
                {
                    error = new
                    {
                        errorCode = 500,
                        errorDateTime = DateTime.UtcNow.ToString("o"),
<<<<<<< HEAD
                        errorMessage = "INTERNAL SERVER ERROR",
=======
                        errorMessage = ex.GetType().Name + " ERROR",
>>>>>>> 1ae0df3 (Automated commit on branch UpdateAccountLambda from AI2DEV)
                        errorDescription = ex.Message
                    }
                };

<<<<<<< HEAD
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
=======
                try
                {
                    var bucket = _config["amazon.s3.bucket"];
                    var region = _config["amazon.s3.region"];
                    var accessKey = _config["secure::amazon.s3.accessKey"];
                    var secretKey = _config["secure::amazon.s3.secretKey"];
                    AWSCredentials creds = null;
                    if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey)) creds = new BasicAWSCredentials(accessKey, secretKey);
                    using (var s3 = creds != null ? new AmazonS3Client(creds, region != null ? Amazon.RegionEndpoint.GetBySystemName(region) : null) : new AmazonS3Client())
                    {
                        if (!string.IsNullOrEmpty(bucket))
                        {
                            var key = (_config["app.name"] ?? "bus-travel-accounts-ea") + "-" + DateTime.UtcNow.ToString("o");
                            var putReq = new PutObjectRequest { BucketName = bucket, Key = key, ContentBody = System.Text.Json.JsonSerializer.Serialize(errorPayload), ContentType = "application/json" };
                            await s3.PutObjectAsync(putReq).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception s3ex)
                {
                    context.Logger.LogLine($"Failed to write error to S3: {s3ex.Message}");
                }

                try
                {
                    using (var cw = new AmazonCloudWatchClient())
                    {
                        var putMetric = new PutMetricDataRequest
                        {
                            Namespace = "GBR_EXCEPTIONS",
                            MetricData = new System.Collections.Generic.List<MetricDatum>
                            {
                                new MetricDatum
                                {
                                    MetricName = "GBR_EXCEPTION_COUNT",
                                    Unit = StandardUnit.Count,
                                    Value = 1,
                                    Dimensions = new System.Collections.Generic.List<Dimension>
                                    {
                                        new Dimension { Name = "HTTP_METHOD", Value = "PUT" },
                                        new Dimension { Name = "GBR_EXCEPTION", Value = (ex.Message ?? "EXCEPTION").Replace(' ','_') }
                                    }
                                }
                            }
                        };
                        await cw.PutMetricDataAsync(putMetric).ConfigureAwait(false);
                    }
                }
                catch (Exception cwex)
                {
                    context.Logger.LogLine($"Failed to send metric: {cwex.Message}");
                }

                var body = System.Text.Json.JsonSerializer.Serialize(errorPayload);
                return new APIGatewayProxyResponse { StatusCode = 500, Body = body, Headers = new System.Collections.Generic.Dictionary<string, string> {{"Content-Type","application/json"}} };
>>>>>>> 1ae0df3 (Automated commit on branch UpdateAccountLambda from AI2DEV)
            }
        }
    }
}
