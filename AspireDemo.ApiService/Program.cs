using AspireDemo.ApiService.EntityFramework;
using AspireDemo.ApiService.Extensions;
using System.Diagnostics;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Reference: .NET Aspire Dashboard & Telemetry
// https://www.youtube.com/watch?v=BB9q0FfVZl4&list=PLdo4fOcmZ0oWMbEO7CiaDZh6cqSTU_lzJ&index=10
// Create custom meter for metrics
var weatherForecastMeter = new Meter(AspireDemo.ServiceDefaults.OpenTelemetry.DefaultMeterName, "1.0.0");
var weatherForecastCounter = weatherForecastMeter.CreateCounter<int>("forecast.count");
// Create custom ActivitySource
var activitySource = new ActivitySource(AspireDemo.ServiceDefaults.OpenTelemetry.DefaultSourceName);


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseCatalogDbMigration();
app.UseCatalogDbDataSeeder();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
{
    // Create a custom activity for the work that we are doing formulating a weather forecast
    using var weatherSpan = activitySource.StartActivity("ForecastActivity");

    var forecast = new WeatherForecast[5];
    for (var i = 0; i < forecast.Length; i++)
    {
        // Increment custom counter
        weatherForecastCounter.Add(1);

        forecast[i] = new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        );

        // Add tags to the Activity
        weatherSpan?.SetTag(forecast[i].Date.ToShortDateString(), forecast[i].Summary);
    }

    weatherSpan?.SetTag("env", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

    // Log a message
    logger.LogInformation("Sending weather forecast: {forecast}", forecast);

    return forecast;
});

app.MapGet("/products", (CatalogDbContext dbContext) =>
{
    return dbContext.Products.ToList();
});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
