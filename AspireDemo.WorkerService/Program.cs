using AspireDemo.WorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient(connectionName: "messaging");

builder.Services.AddHostedService<NotificationsHandler>();

var host = builder.Build();
host.Run();
