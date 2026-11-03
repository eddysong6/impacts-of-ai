using ImpactsOfAI.Api.ApiKey;
using ImpactsOfAI.Api.Endpoints.Article;

#pragma warning disable OPENAI001

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddTransient<IApiKeyValidator, ApiKeyValidator>();

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapArticlesEndpoints();

app.Run();
