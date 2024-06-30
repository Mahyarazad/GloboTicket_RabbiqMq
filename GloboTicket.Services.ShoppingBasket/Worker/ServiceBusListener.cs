using GloboTicket.Services.ShoppingBasket.Models;
using GloboTicket.Services.ShoppingBasket.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using GloboTicket.Integration;

namespace GloboTicket.Services.ShoppingBasket.Worker
{
    public class ServiceBusListener : IHostedService
    {
        private readonly IConfiguration configuration;
        private readonly IConnection _connection;
        private readonly IModel _client;
        private readonly EventingBasicConsumer _eventListiner;
        private readonly BasketLinesIntegrationRepository basketLinesRepository;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly string priceUpdatedMessageTopic;
        public ServiceBusListener(IConfiguration configuration, BasketLinesIntegrationRepository basketLinesRepository)
        {
            this.configuration = configuration;
            this.basketLinesRepository = basketLinesRepository;
            priceUpdatedMessageTopic = configuration.GetValue<string>("PriceUpdatedMessageTopic");
            var connectionFactory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = connectionFactory.CreateConnection();
            _client = _connection.CreateModel();
            _client.ExchangeDeclare(configuration.GetValue<string>("Topic_Name"), type: ExchangeType.Topic);
            _client.QueueDeclare(priceUpdatedMessageTopic, true, false, false, null);
            _client.QueueBind(priceUpdatedMessageTopic, configuration.GetValue<string>("Topic_Name"), "payment.*");
            _eventListiner = new EventingBasicConsumer(_client);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {

            _eventListiner.Received += async (s, a) =>
            {
                await ProcessMessageAsync(Serializer<PriceUpdate>.Deserialize(a.Body.ToArray()), a.DeliveryTag, _cancellationTokenSource.Token);
            };

            _client.BasicConsume(priceUpdatedMessageTopic, true, _eventListiner);

            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(PriceUpdate message, ulong deliveryTag ,CancellationToken token)
        {

            await basketLinesRepository.UpdatePricesForIntegrationEvent(message);

            //_client.BasicAck(deliveryTag, false);

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Dispose();
        }

        protected void ProcessError(Exception e)
        {
        }
    }
}
