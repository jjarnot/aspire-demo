namespace AspireDemo.WorkerService;

using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class EventsMessageHandler : IHostedService
{
    private readonly ILogger<EventsMessageHandler> _logger;
    private readonly IConfiguration _config;
    private readonly IConnection _messageConnection;
    private IModel? _messageChannel;
    private EventingBasicConsumer consumer;

    public EventsMessageHandler(ILogger<EventsMessageHandler> logger, IConfiguration config, IConnection messageConnection)
    {
        _logger = logger;
        _config = config;
        _messageConnection = messageConnection;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Message handler is staring...");

        var queueName = _config.GetValue<string>("MESSAGING:EVENTSQUEUE");
        _messageChannel = _messageConnection.CreateModel();
        _messageChannel.QueueDeclare(queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        consumer = new EventingBasicConsumer(_messageChannel);
        consumer.Received += ProcessMessageAsync;

        _messageChannel.BasicConsume(queue: queueName,
            autoAck: true,
            consumer: consumer);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Message handler is stoping...");
        consumer.Received -= ProcessMessageAsync;
        _messageChannel?.Close();
        _messageConnection.Close();
        return Task.CompletedTask;
    }

    private void ProcessMessageAsync(object? sender, BasicDeliverEventArgs args)
    {
        string messagetext = Encoding.UTF8.GetString(args.Body.ToArray());
        _logger.LogInformation("Received message text: {text}", messagetext);
    }
}