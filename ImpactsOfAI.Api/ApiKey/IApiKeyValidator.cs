namespace ImpactsOfAI.Api.ApiKey;

public interface IApiKeyValidator
{
    bool IsValid(string apiKey);
}