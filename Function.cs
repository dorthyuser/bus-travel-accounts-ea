using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HealthAliveLambda
{
    public class Function
    {
        // Simple health check - returns UP similar to Mule /alive flow
        public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Health /alive requested");
            return Task.FromResult(new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = "UP",
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            });
        }
    }
}
