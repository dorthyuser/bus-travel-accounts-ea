using System;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using System.Text;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PutAccountLambda
{
    public class Function
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly IConfiguration _config;
        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonCloudWatch _cwClient;

        public Function()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            _config = builder.Build();
            _s3Client = new AmazonS3Client();
            _cwClient = new AmazonCloudWatchClient();
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var headers = request?.Headers ?? new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var basicDetails = new
            {
                CUSTOMER_CORRELATION_ID = headers.ContainsKey("CUSTOMER_CORRELATION_ID") ? headers["CUSTOMER_CORRELATION_ID"].Trim() : "CUSTOMER_CORRELATION_ID_NOT_FOUND",
                X_CORRELATION_ID = context?.AwsRequestId ?? Guid.NewGuid().ToString(),
                client_id = headers.ContainsKey("client_id") ? headers["client_id"].Trim() : null,
                httpMethod = request?.HttpMethod,
                relativePath = request?.Path
            };

            context.Logger.LogLine($"START - Request received: {System.Text.Json.JsonSerializer.Serialize(basicDetails)}");

            try
            {
                context.Logger.LogLine($"Before Request - bus-travel-accounts-sa-update-account - AccountId: {request?.PathParameters?["id"]}");

                var host = _config["https.requester.sa.accounts.host"] ?? string.Empty;
                var port = _config["https.requester.sa.accounts.port"] ?? string.Empty;
                var basePath = _config["https.requester.sa.accounts.basepath"] ?? string.Empty;
                var accountId = request?.PathParameters != null && request.PathParameters.ContainsKey("id") ? request.PathParameters["id"] : string.Empty;

                var uri = new UriBuilder
                {
                    Scheme = "https",
                    Host = host,
                    Port = string.IsNullOrEmpty(port) ? -1 : int.Parse(port),
                    Path = CombinePaths(basePath, $"accounts/{Uri.EscapeDataString(accountId ?? string.Empty)}")
                };

                using var req = new HttpRequestMessage(HttpMethod.Put, uri.Uri);
                req.Headers.Add("CUSTOMER_CORRELATION_ID", basicDetails.CUSTOMER_CORRELATION_ID);
                req.Headers.Add("X_CORRELATION_ID", basicDetails.X_CORRELATION_ID);
                var clientSecret = _config["secure::https.requester.sa.accounts.client_secret"] ?? _config["https.requester.sa.accounts.client_secret"];
                var clientId = _config["https.requester.sa.accounts.client_id"] ?? string.Empty;
                if (!string.IsNullOrEmpty(clientSecret)) req.Headers.Add("client_secret", clientSecret);
                if (!string.IsNullOrEmpty(clientId)) req.Headers.Add("client_id", clientId);

                req.Content = new StringContent(request?.Body ?? string.Empty, Encoding.UTF8, request?.Headers != null && request.Headers.ContainsKey("Content-Type") ? request.Headers["Content-Type"] : "application/json");

                var response = await _httpClient.SendAsync(req);
                var responseBody = await response.Content.ReadAsStringAsync();

                context.Logger.LogLine($"After Request - bus-travel-accounts-sa-update-account - httpStatus: {(int)response.StatusCode}");

                int httpStatus = (int)response.StatusCode;

                return new APIGatewayProxyResponse
                {
                    StatusCode = httpStatus,
                    Body = responseBody,
                    Headers = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "Content-Type", response.Content.Headers.ContentType?.ToString() ?? "application/json" }
                    }
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message}");
                var errorPayload = new
                {
                    error = new
                    {
                        errorCode = 500,
                        errorDateTime = DateTime.UtcNow,
                        errorMessage = ex.GetType().Name + " ERROR",
                        errorDescription = ex.Message
                    }
                };

                try
                {
                    await _cwClient.PutMetricDataAsync(new PutMetricDataRequest
                    {
                        Namespace = "GBR_EXCEPTIONS",
                        MetricData = new System.Collections.Generic.List<MetricDatum>
                        {
                            new MetricDatum
                            {
                                MetricName = "GBR_EXCEPTION_COUNT",
                                Value = 1,
                                Dimensions = new System.Collections.Generic.List<Dimension> {
                                    new Dimension { Name = "HTTP_METHOD", Value = request?.HttpMethod ?? "PUT" },
                                    new Dimension { Name = "GBR_EXCEPTION", Value = (errorPayload.error.errorMessage ?? "EXCEPTION").Replace(" ", "_") }
                                }
                            }
                        }
                    });
                }
                catch (Exception cwex)
                {
                    context.Logger.LogLine("CloudWatch metric put failed: " + cwex.Message);
                }

                try
                {
                    var bucket = _config["amazon.s3.bucket"];
                    var key = (_config["json.logger.application.name"] ?? "bus-travel-accounts-ea") + "-" + DateTime.UtcNow.ToString("o");
                    var s3Content = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        errorCode = errorPayload.error.errorCode,
                        httpMethod = request?.HttpMethod,
                        httpErrorCode = 500,
                        errorMessage = errorPayload.error.errorMessage,
                        errorDescription = errorPayload.error.errorDescription,
                        timestamp = errorPayload.error.errorDateTime,
                        apiName = _config["json.logger.application.name"],
                        endpoint = request?.Path
                    });

                    if (!string.IsNullOrEmpty(bucket))
                    {
                        var putReq = new PutObjectRequest
                        {
                            BucketName = bucket,
                            Key = key,
                            ContentBody = s3Content
                        };
                        await _s3Client.PutObjectAsync(putReq);
                        context.Logger.LogLine("Created error in S3 bucket.");
                    }
                }
                catch (Exception s3ex)
                {
                    context.Logger.LogLine("Failed to write error to S3: " + s3ex.Message);
                }

                var body = System.Text.Json.JsonSerializer.Serialize(errorPayload);
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = body,
                    Headers = new System.Collections.Generic.Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
        }

        private static string CombinePaths(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b?.TrimStart('/') ?? string.Empty;
            if (string.IsNullOrEmpty(b)) return a;
            return (a.TrimEnd('/') + "/" + b.TrimStart('/')).TrimStart('/');
        }
    }
}
