namespace ImpactsOfAI.Api.Shared;

public static class ValidationHelper
{
    public static HttpValidationProblemDetails CreateValidationProblemDetails(IDictionary<string, string[]> errors, string instance)
    {
        return new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more validation errors occurred.",
            Instance = instance
        };
    }
}