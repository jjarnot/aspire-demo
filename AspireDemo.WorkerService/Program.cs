using AspireDemo.WorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient("messaging");

builder.Services.AddHostedService<NotificationsHandler>();

var host = builder.Build();
host.Run();
