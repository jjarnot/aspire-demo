using AspireDemo.ApiService.EntityFramework;
using AspireDemo.ApiService.Extensions;
using RabbitMQ.Client;
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

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

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
