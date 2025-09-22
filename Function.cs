using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PostAccountsLambda
{
    public class Function
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly IConfiguration _configuration;

        public Function()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
            _configuration = builder.Build();
        }

        // Handler for POST /accounts
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string customerCorrelationId = "CUSTOMER_CORRELATION_ID_NOT_FOUND";
            if (request.Headers != null && request.Headers.ContainsKey("CUSTOMER_CORRELATION_ID"))
                customerCorrelationId = request.Headers["CUSTOMER_CORRELATION_ID"]?.Trim() ?? customerCorrelationId;

            string xCorrelationId = Guid.NewGuid().ToString();
            if (request.Headers != null && request.Headers.ContainsKey("X_CORRELATION_ID"))
                xCorrelationId = request.Headers["X_CORRELATION_ID"] ?? xCorrelationId;

            context.Logger.LogLine($"START - Request received, X_CORRELATION_ID={xCorrelationId}");

            try
            {
                string basePath = _configuration["https.requester.sa.accounts.basepath"];
                if (string.IsNullOrEmpty(basePath))
                {
                    string host = _configuration["https.requester.sa.accounts.host"];
                    string port = _configuration["https.requester.sa.accounts.port"];
                    if (!string.IsNullOrEmpty(host))
                    {
                        basePath = port == "443" || string.IsNullOrEmpty(port) ? $"https://{host}" : $"https://{host}:{port}";
                    }
                }

                string requestUri = basePath.TrimEnd('/') + "/accounts";

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
                httpRequest.Headers.Add("CUSTOMER_CORRELATION_ID", customerCorrelationId);
                httpRequest.Headers.Add("X_CORRELATION_ID", xCorrelationId);
                var clientSecret = _configuration["secure::https.requester.sa.accounts.client_secret"];
                var clientId = _configuration["https.requester.sa.accounts.client_id"];
                if (!string.IsNullOrEmpty(clientSecret)) httpRequest.Headers.Add("client_secret", clientSecret);
                if (!string.IsNullOrEmpty(clientId)) httpRequest.Headers.Add("client_id", clientId);

                string body = request.Body ?? string.Empty;
                httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");

                context.Logger.LogLine($"Before Request - bus-travel-accounts-sa-create-Accounts, url={requestUri}");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseBody = await response.Content.ReadAsStringAsync();

                context.Logger.LogLine($"After Request - status={(int)response.StatusCode}");

                // If created (201) return as mule flow
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Body = responseBody,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error - {ex}");
                var payload = new
                {
                    error = new
                    {
                        errorCode = 500,
                        errorDateTime = DateTime.UtcNow,
                        errorMessage = "INTERNAL SERVER ERROR",
                        errorDescription = ex.Message
                    }
                };
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = JsonConvert.SerializeObject(payload),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
        }
    }
}
