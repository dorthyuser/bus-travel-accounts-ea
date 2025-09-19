using System;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.S3.Model;
using System.Text;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CreateAccountLambda
{
    public class Function
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _client;

        public Function()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
            _client = new HttpClient();
            if (int.TryParse(_config["https.requester.sa.accounts.response.timeout"], out var to) && to > 0)
            {
                _client.Timeout = TimeSpan.FromMilliseconds(to);
            }
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var xCorrelation = request.Headers != null && request.Headers.ContainsKey("X_CORRELATION_ID") ? request.Headers["X_CORRELATION_ID"] : Guid.NewGuid().ToString();
            var basicCorrelation = request.Headers != null && request.Headers.ContainsKey("CUSTOMER_CORRELATION_ID") ? request.Headers["CUSTOMER_CORRELATION_ID"] : "CUSTOMER_CORRELATION_ID_NOT_FOUND";

            context.Logger.LogLine($"START - Create Account. X_CORRELATION_ID={xCorrelation}");

            try
            {
                context.Logger.LogLine($"Before Request - bus-travel-accounts-sa-create-Accounts");

                var host = _config["https.requester.sa.accounts.host"] ?? "";
                var port = _config["https.requester.sa.accounts.port"] ?? "";
                var basePath = _config["https.requester.sa.accounts.basepath"] ?? string.Empty;
                if (!basePath.StartsWith("/")) basePath = "/" + basePath;
                if (basePath.EndsWith("/")) basePath = basePath.TrimEnd('/');

                var scheme = "https";
                var url = new StringBuilder();
                url.Append(scheme).Append("://").Append(host);
                if (!string.IsNullOrEmpty(port)) url.Append(":").Append(port);
                url.Append(basePath).Append("/accounts");

                var req = new HttpRequestMessage(HttpMethod.Post, url.ToString());
                req.Headers.Add("CUSTOMER_CORRELATION_ID", basicCorrelation);
                req.Headers.Add("X_CORRELATION_ID", xCorrelation);
                var clientSecret = _config["secure::https.requester.sa.accounts.client_secret"];
                if (!string.IsNullOrEmpty(clientSecret)) req.Headers.Add("client_secret", clientSecret);
                var clientId = _config["https.requester.sa.accounts.client_id"];
                if (!string.IsNullOrEmpty(clientId)) req.Headers.Add("client_id", clientId);

                // forward payload as-is
                req.Content = new StringContent(request.Body ?? string.Empty, Encoding.UTF8, request.Headers != null && request.Headers.ContainsKey("Content-Type") ? request.Headers["Content-Type"] : "application/json");

                var resp = await _client.SendAsync(req);
                var respBody = await resp.Content.ReadAsStringAsync();

                context.Logger.LogLine($"After Request - bus-travel-accounts-sa-create-Accounts. httpStatus={(int)resp.StatusCode}");

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)resp.StatusCode,
                    Body = respBody,
                    Headers = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "Content-Type", resp.Content.Headers.ContentType?.ToString() ?? "application/json" },
                        { "X_CORRELATION_ID", xCorrelation }
                    }
                };
            }
            catch (HttpRequestException hre)
            {
                context.Logger.LogLine($"Error HttpRequestException: {hre.Message}");
                var payload = new
                {
                    error = new
                    {
                        errorCode = 502,
                        errorDateTime = DateTime.UtcNow,
                        errorMessage = "BAD_GATEWAY",
                        errorDescription = hre.Message
                    }
                };
                await WriteErrorToS3Async(payload, request, context);
                return new APIGatewayProxyResponse { StatusCode = 502, Body = System.Text.Json.JsonSerializer.Serialize(payload), Headers = new System.Collections.Generic.Dictionary<string, string> { { "Content-Type", "application/json" } } };
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error Exception: {ex.Message}");
                var payload = new
                {
                    error = new
                    {
                        errorCode = 500,
                        errorDateTime = DateTime.UtcNow,
                        errorMessage = ex.GetType().Name + " ERROR",
                        errorDescription = ex.Message
                    }
                };
                await WriteErrorToS3Async(payload, request, context);
                return new APIGatewayProxyResponse { StatusCode = 500, Body = System.Text.Json.JsonSerializer.Serialize(payload), Headers = new System.Collections.Generic.Dictionary<string, string> { { "Content-Type", "application/json" } } };
            }
        }

        private async Task WriteErrorToS3Async(object payload, APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                var bucket = _config["amazon.s3.bucket"];
                if (string.IsNullOrEmpty(bucket))
                {
                    context.Logger.LogLine("S3 bucket not configured, skipping S3 upload.");
                    return;
                }

                var accessKey = _config["secure::amazon.s3.accessKey"];
                var secretKey = _config["secure::amazon.s3.secretKey"];
                var region = _config["amazon.s3.region"] ?? "us-east-1";

                var creds = (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
                    ? new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey)
                    : null;

                AmazonS3Client s3Client;
                if (creds != null)
                {
                    s3Client = new AmazonS3Client(creds, Amazon.RegionEndpoint.GetBySystemName(region));
                }
                else
                {
                    s3Client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region));
                }

                var key = (_config["json.logger.application.name"] ?? "bus-travel-accounts-ea") + "-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + ".json";
                var content = System.Text.Json.JsonSerializer.Serialize(new { payload, request = new { request.Path, request.HttpMethod, request.Body } });

                var putReq = new PutObjectRequest { BucketName = bucket, Key = key, ContentBody = content };
                await s3Client.PutObjectAsync(putReq);
                context.Logger.LogLine("Uploaded error payload to S3: " + key);
            }
            catch (Exception s3ex)
            {
                context.Logger.LogLine("Failed to write error to S3: " + s3ex.Message);
            }
        }
    }
}
