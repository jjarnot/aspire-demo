using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var rabbitmq = builder.AddRabbitMQ("messaging").WithManagementPlugin();

var eventsQueue = builder.Configuration.GetValue<string>("Messaging:EventsQueue");

var postgres = builder
        .AddPostgres("postgres")
        .WithPgAdmin()
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.AspireDemo_ApiService>("apiservice")
                .WithReference(catalogDb)
                .WaitFor(catalogDb)
                .WithEnvironment("MESSAGING__EVENTSQUEUE", eventsQueue)
                .WithReference(rabbitmq)
                .WaitFor(rabbitmq);

builder.AddProject<Projects.AspireDemo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(apiService);

builder.AddProject<Projects.AspireDemo_WorkerService>("workerservice")
       .WithEnvironment("MESSAGING__EVENTSQUEUE", eventsQueue)
       .WithReference(rabbitmq)
       .WaitFor(rabbitmq);

builder.Build().Run();
