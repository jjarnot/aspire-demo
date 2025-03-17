using AspireDemo.ApiService.EntityFramework;
using AspireDemo.ApiService.Extensions;
using RabbitMQ.Client;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Text;
using AspireDemo.ServiceDefaults;
using System.Text.Json;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using AspireDemo.ApiService;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<ProductDbContext>(connectionName: "catalogdb");

builder.AddRabbitMQClient(connectionName: "messaging");

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var weatherForecastMeter = new Meter(AspireDemo.ServiceDefaults.OpenTelemetry.DefaultMeterName, "1.0.0");
var weatherForecastCounter = weatherForecastMeter.CreateCounter<int>("forecast.count");
var activitySource = new ActivitySource(AspireDemo.ServiceDefaults.OpenTelemetry.DefaultSourceName);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// ProductDb Migration and seed
app.UseProductDbMigration();
app.UseProductDbDataSeeder();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
{
    using var span = activitySource.StartActivity("ForecastActivity");
    var forecats = new WeatherForecast[5];
    for (var i = 0; i < forecats.Length; i++)
    {
        weatherForecastCounter.Add(1);

        forecats[i] = new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        );
        span?.SetTag("Env", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
    }
    logger.LogInformation("Sending weather forecats: {foreacts}", forecats);
    return forecats;
})
.WithName("GetWeatherForecast");

app.MapGet("/products", (ProductDbContext dbContext) =>
{
    return dbContext.Products.ToList();
});

app.MapPost("/notifications", async (IConnection connection, IConfiguration config, string message, ILogger<Program> logger) =>
{
    var eventsQueue = config.GetValue<string>("MESSAGING:NOTIFICATIONSQUEUE");

    // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
    // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
    var activityName = $"{eventsQueue} send";

    // reference: https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/examples/MicroserviceExample/Utils/Messaging/MessageSender.cs
    using var span = activitySource.StartActivity(activityName, ActivityKind.Producer);
    span?.SetTag("env", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

    using var channel = RabbitMqHelper.CreateModelAndDeclareQueue(connection, eventsQueue!);
    var basicProperties = channel.CreateBasicProperties();

    ActivityContext contextToInject = default;
    if (span != null)
    {
        contextToInject = span.Context;
    }
    else if (Activity.Current != null)
    {
        contextToInject = Activity.Current.Context;
    }

    // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
    Propagators.DefaultTextMapPropagator.Inject(new PropagationContext(contextToInject, Baggage.Current), basicProperties, (IBasicProperties props, string key, string value) =>
    {
        try
        {
            if (props.Headers == null)
            {
                props.Headers = new Dictionary<string, object>();
            }

            props.Headers[key] = value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to inject trace context.");
        }
    });

    logger.LogInformation("Sending message text: {text}", message);
    
    channel.BasicPublish(exchange: string.Empty,
                         routingKey: eventsQueue,
                         mandatory: false,
                         basicProperties: basicProperties,
                         body: Encoding.UTF8.GetBytes(message));
});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}