using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
        .AddPostgres("postgres")
        .WithPgAdmin()
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");


const string NotificationsQueueEnvName = "MESSAGING__NOTIFICATIONSQUEUE";

var notificationsQueue = builder.Configuration.GetValue<string>("Messaging:NotificationsQueue");
var rabbitmq = builder.AddRabbitMQ("messaging").WithManagementPlugin();

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.AspireDemo_ApiService>("apiservice")
                        .WithReference(catalogDb)
                        .WaitFor(catalogDb)
                        .WithReference(rabbitmq)
                        .WaitFor(rabbitmq)
                        .WithEnvironment(NotificationsQueueEnvName, notificationsQueue);

builder.AddProject<Projects.AspireDemo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.AspireDemo_WorkerService>("workerservice")
        .WithReference(rabbitmq)
        .WaitFor(rabbitmq)
        .WithEnvironment(NotificationsQueueEnvName, notificationsQueue);

builder.Build().Run();
