using AspireDemo.WorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient(connectionName: "messaging");

builder.Services.AddHostedService<EventsMessageHandler>();

var host = builder.Build();
host.Run();
