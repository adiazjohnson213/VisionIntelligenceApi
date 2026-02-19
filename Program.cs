using System.Text.Json.Serialization;
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Azure.Core;
using Microsoft.Extensions.Options;
using VisionIntelligenceAPI.Clients;
using VisionIntelligenceAPI.Commons;
using VisionIntelligenceAPI.Observability;
using VisionIntelligenceAPI.Services;

var builder = WebApplication.CreateBuilder(args);

//Bind configuration settings
builder.Services.AddOptions<VisionOptions>()
    .Bind(builder.Configuration.GetSection("Vision"))
    .ValidateDataAnnotations()
    .Validate(o => Uri.TryCreate(o.Endpoint, UriKind.Absolute, out _), "Vision:Endpoint must be an absolute URI.")
    .ValidateOnStart();

builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<VisionOptions>>().Value);

builder.Services.AddHttpClient<VisionRestClient>();


var clientOptions = new ImageAnalysisClientOptions()
{
    Retry =
    {
        MaxRetries = 5,
        Mode = RetryMode.Exponential,
        Delay = TimeSpan.FromSeconds(0.8),
        MaxDelay = TimeSpan.FromSeconds(8)
    }
};

// Add services to the container.
builder.Services.AddSingleton(sp =>
{
    var opt = sp.GetRequiredService<IOptions<VisionOptions>>().Value;
    return new ImageAnalysisClient(new Uri(opt.Endpoint), new AzureKeyCredential(opt.ApiKey), clientOptions);
});
builder.Services.AddScoped<VisionAnalysisService>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
