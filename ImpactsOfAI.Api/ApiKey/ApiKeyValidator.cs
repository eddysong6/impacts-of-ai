namespace ImpactsOfAI.Api.ApiKey;

public class ApiKeyValidator(IConfiguration configuration) : IApiKeyValidator
{
    private readonly IConfiguration configuration = configuration;

    public bool IsValid(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;
        
        string? validApiKey = configuration["ApiKey"];

        return validApiKey != null && apiKey == validApiKey;
    }
}