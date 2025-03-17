using RabbitMQ.Client;

namespace AspireDemo.ApiService;

public static class RabbitMqHelper
{
    public static IModel CreateModelAndDeclareQueue(IConnection connection, string queueName)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrEmpty(queueName);

        var model = connection.CreateModel();
        model.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        return model;
    }
}