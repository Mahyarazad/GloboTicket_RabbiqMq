using GloboTicket.Integration.Messages;
using RabbitMQ.Client;
using System;

namespace GloboTicket.Integration.MessagingBus
{
    public class RabbitMqMessageServiceBus: IMessageBus
    {
        private readonly IConnection _connection;
        private readonly IModel _client;
        public RabbitMqMessageServiceBus()
        {
            // read from secret
            var connectionFactory = new ConnectionFactory()
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            _connection = connectionFactory.CreateConnection();
            _client = _connection.CreateModel();
        }
        public void PublishMessage(IntegrationBaseMessage message, string topicName, string queueName ,string routingkey)
        {
            _client.ExchangeDeclare(topicName, type: ExchangeType.Topic);
            _client.QueueDeclare(routingkey, true, false, false, null);
            _client.QueueBind(queueName, topicName, routingkey);
            var properties = _client.CreateBasicProperties();
            properties.CorrelationId = Guid.NewGuid().ToString();


            _client.BasicPublish(topicName, routingkey, properties, Serializer<IntegrationBaseMessage>.Serialize(message));
            Console.WriteLine($"Sent message to {topicName}");          
        }
    }
}
