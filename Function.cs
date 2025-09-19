using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CreateAccountLambda
{
    public class Function
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly IConfiguration _config;

        public Function()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
            _config = builder.Build();
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var correlationId = request?.Headers != null && request.Headers.ContainsKey("X_CORRELATION_ID") ? request.Headers["X_CORRELATION_ID"] : Guid.NewGuid().ToString();
            var customerCorrelationId = request?.Headers != null && request.Headers.ContainsKey("CUSTOMER_CORRELATION_ID") ? request.Headers["CUSTOMER_CORRELATION_ID"] : "CUSTOMER_CORRELATION_ID_NOT_FOUND";

            try
            {
                context.Logger.LogLine($"START - Request received. X_CORRELATION_ID={correlationId} CUSTOMER_CORRELATION_ID={customerCorrelationId}");
                context.Logger.LogLine($"Before Request - bus-travel-accounts-sa-create-Accounts");

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
                sb.Append("accounts");
                var url = sb.ToString();

                var payload = request?.Body ?? string.Empty;
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Add("CUSTOMER_CORRELATION_ID", customerCorrelationId);
                httpRequest.Headers.Add("X_CORRELATION_ID", correlationId);
                if (!string.IsNullOrEmpty(clientId)) httpRequest.Headers.Add("client_id", clientId);
                if (!string.IsNullOrEmpty(clientSecret)) httpRequest.Headers.Add("client_secret", clientSecret);

                var response = await _httpClient.SendAsync(httpRequest).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                context.Logger.LogLine($"After Request - bus-travel-accounts-sa-create-Accounts. httpStatus={(int)response.StatusCode}");

                // Mule expects 201 for create
                if ((int)response.StatusCode == 201)
                {
                    return new APIGatewayProxyResponse { StatusCode = 201, Body = responseBody, Headers = new System.Collections.Generic.Dictionary<string, string> {{"Content-Type","application/json"}} };
                }
                else
                {
                    return new APIGatewayProxyResponse { StatusCode = (int)response.StatusCode, Body = responseBody, Headers = new System.Collections.Generic.Dictionary<string, string> {{"Content-Type","application/json"}} };
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message}");
                var errorPayload = new
                {
                    error = new
                    {
                        errorCode = 500,
                        errorDateTime = DateTime.UtcNow.ToString("o"),
                        errorMessage = ex.GetType().Name + " ERROR",
                        errorDescription = ex.Message
                    }
                };

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
                                        new Dimension { Name = "HTTP_METHOD", Value = "POST" },
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
            }
        }
    }
}
