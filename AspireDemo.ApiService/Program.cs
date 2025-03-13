using AspireDemo.ApiService.EntityFramework;
using AspireDemo.ApiService.Extensions;
using RabbitMQ.Client;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<ProductDbContext>(connectionName: "catalogdb");

builder.AddRabbitMQClient(connectionName: "messaging");

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var weatherForecastMeter = new Meter("weather.backend", "1.0.0");
var weatherForecastCount = weatherForecastMeter.CreateCounter<int>("forecast.count");
var activitySource = new ActivitySource("weather.backend");

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
    using Activity? weatherSpan = activitySource.StartActivity("ForecastActivity");
    var forecats = new WeatherForecast[5];
    for (var i = 0; i < forecats.Length; i++)
    {
        weatherForecastCount.Add(1);
        forecats[i] = new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        );
        weatherSpan?.SetTag("Env", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
    }
    logger.LogInformation("Sending weather forecats: {foreacts}", forecats);
    return forecats;
})
.WithName("GetWeatherForecast");

app.MapGet("/products", (ProductDbContext dbContext) =>
{
    return dbContext.Products.ToList();
});

app.MapPost("/notify", static async (IConnection connection, IConfiguration config, string message) =>
{
    var aspireEventsQueue = config.GetValue<string>("MESSAGING:EVENTSQUEUE");

    using var channel = connection.CreateModel();
    channel.QueueDeclare(queue: aspireEventsQueue,
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

    var body = Encoding.UTF8.GetBytes(message);

    channel.BasicPublish(exchange: string.Empty,
                         routingKey: aspireEventsQueue,
                          mandatory: false,
                         basicProperties: null,
                         body: body);
    Console.WriteLine("A message has been published to the queue.");
});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
