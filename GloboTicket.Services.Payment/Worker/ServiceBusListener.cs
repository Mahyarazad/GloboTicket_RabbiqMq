using GloboTicket.Integration;
using GloboTicket.Integration.Messages;
using GloboTicket.Integration.MessagingBus;
using GloboTicket.Services.Payment.Messages;
using GloboTicket.Services.Payment.Model;
using GloboTicket.Services.Payment.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GloboTicket.Services.Payment.Worker
{
    public class ServiceBusListener : IHostedService
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;
        private readonly IConnection _connection;
        private readonly IModel _client;
        private readonly EventingBasicConsumer _eventListiner;
        private readonly IMessageBus messageBus;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IExternalGatewayPaymentService externalGatewayPaymentService;
        private readonly string orderPaymentUpdatedMessageTopic;

        public ServiceBusListener(IConfiguration configuration, ILoggerFactory loggerFactory, IExternalGatewayPaymentService externalGatewayPaymentService, IMessageBus messageBus)
        {
            logger = loggerFactory.CreateLogger<ServiceBusListener>();
            orderPaymentUpdatedMessageTopic = configuration.GetValue<string>("OrderPaymentUpdatedMessageTopic");

            this.configuration = configuration;
            this.externalGatewayPaymentService = externalGatewayPaymentService;
            this.messageBus = messageBus;

            var connectionFactory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = connectionFactory.CreateConnection();
            _client = _connection.CreateModel();
            _client.ExchangeDeclare(configuration.GetValue<string>("Topic_Name"), type: ExchangeType.Topic);
            _client.QueueDeclare(orderPaymentUpdatedMessageTopic, true, false, false, null);
            _client.QueueBind(orderPaymentUpdatedMessageTopic, configuration.GetValue<string>("Topic_Name"), "payment.order");
            _eventListiner = new EventingBasicConsumer(_client);
            
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {

            _eventListiner.Received += async (s, a) =>
            {
                await ProcessMessageAsync(Serializer<OrderPaymentRequestMessage>.Deserialize(a.Body.ToArray()),a.DeliveryTag, _cancellationTokenSource.Token);
            };

            _client.BasicConsume(orderPaymentUpdatedMessageTopic, true, _eventListiner);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug($"ServiceBusListener stopping.");
            this._client.Dispose();
        }

        protected void ProcessError(Exception e)
        {
            logger.LogError(e, "Error while processing queue item in ServiceBusListener.");
        }

        protected async Task ProcessMessageAsync(OrderPaymentRequestMessage orderPaymentRequestMessage, ulong deliveryTag ,CancellationToken token)
        {

            PaymentInfo paymentInfo = new PaymentInfo
            {
                CardNumber = orderPaymentRequestMessage.CardNumber,
                CardName = orderPaymentRequestMessage.CardName,
                CardExpiration = orderPaymentRequestMessage.CardExpiration,
                Total = orderPaymentRequestMessage.Total
            };

            var result = await externalGatewayPaymentService.PerformPayment(paymentInfo);

            _client.BasicAck(deliveryTag, false);

            //send payment result to order service via service bus
            OrderPaymentUpdateMessage orderPaymentUpdateMessage = new OrderPaymentUpdateMessage
            {
                PaymentSuccess = result, 
                OrderId = orderPaymentRequestMessage.OrderId
            };

            try
            {
                await messageBus.PublishMessage(orderPaymentUpdateMessage, configuration.GetValue<string>("Topic_Name"), "orderpaymentrequestmessage", "payment.order");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

            logger.LogDebug($"{orderPaymentRequestMessage.OrderId}: ServiceBusListener received item.");
            await Task.Delay(20000);
            logger.LogDebug($"{orderPaymentRequestMessage.OrderId}:  ServiceBusListener processed item.");
        }
    }
}
