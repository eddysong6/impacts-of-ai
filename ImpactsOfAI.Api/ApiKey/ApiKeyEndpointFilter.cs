using ImpactsOfAI.Api.Shared;

namespace ImpactsOfAI.Api.ApiKey;

public class ApiKeyEndpointFilter(IApiKeyValidator apiKeyValidator) : IEndpointFilter
{
    private readonly IApiKeyValidator apiKeyValidator = apiKeyValidator;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        string apiKey = context.HttpContext.Request.Query["apiKey"].ToString();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            HttpValidationProblemDetails problemDetails = ValidationHelper.CreateValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    { "apiKey", ["The API key must be specified."] }
                }, context.HttpContext.Request.Path);
            
            return Results.BadRequest(problemDetails);
        }

        if (!apiKeyValidator.IsValid(apiKey))
            return Results.Unauthorized();
        
        return await next(context);
    }
}