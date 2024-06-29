using GloboTicket.Integration.MessagingBus;
using GloboTicket.Services.Ordering.Entities;
using GloboTicket.Services.Ordering.Messages;
using GloboTicket.Services.Ordering.Repositories;


using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GloboTicket.Services.Ordering.Messaging
{
    public class RabbitMQConsumer: IRabbitMqConsumer
    {
        private readonly string subscriptionName = "globoticketorder";
        //private readonly IReceiverClient checkoutMessageReceiverClient;
        //private readonly IReceiverClient orderPaymentUpdateMessageReceiverClient;
        private readonly IConnection _connection;
        private readonly IModel orderPaymentUpdateMessageReceiverClient;
        private readonly IModel checkoutMessageReceiverClient;

        private readonly IConfiguration _configuration;

        private readonly OrderRepository _orderRepository;
        private readonly IMessageBus _messageBus;

        private readonly string checkoutMessageTopic;
        private readonly string orderPaymentRequestMessageTopic;
        private readonly string orderPaymentUpdatedMessageTopic;

        public RabbitMQConsumer(IConfiguration configuration, OrderRepository orderRepository)
        {
            _configuration = configuration;
            _orderRepository = orderRepository;
            // _logger = logger;
            //_messageBus = messageBus;

            var connectionFactory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMq:Host"],
                UserName = _configuration["RabbitMq:Username"],
                Password = _configuration["RabbitMq:Password"],
            };

            _connection = connectionFactory.CreateConnection();
            orderPaymentUpdateMessageReceiverClient = _connection.CreateModel();
            checkoutMessageReceiverClient = _connection.CreateModel();

            orderPaymentUpdateMessageReceiverClient.ExchangeDeclare(exchange: _configuration.GetValue<string>("Topic_Name"), type: ExchangeType.Topic);
            checkoutMessageReceiverClient.ExchangeDeclare(exchange: _configuration.GetValue<string>("Topic_Name"), type: ExchangeType.Topic);

            //var serviceBusConnectionString = _configuration.GetValue<string>("ServiceBusConnectionString");
            checkoutMessageTopic = _configuration.GetValue<string>("CheckoutMessageTopic");
            orderPaymentRequestMessageTopic = _configuration.GetValue<string>("OrderPaymentRequestMessageTopic");


            orderPaymentUpdateMessageReceiverClient.QueueDeclare(orderPaymentRequestMessageTopic, true, false,false , null);
            checkoutMessageReceiverClient.QueueDeclare(checkoutMessageTopic, true, false, false, null);
            //checkoutMessageReceiverClient = new SubscriptionClient(serviceBusConnectionString, checkoutMessageTopic, subscriptionName);
            //orderPaymentUpdateMessageReceiverClient = new SubscriptionClient(serviceBusConnectionString, orderPaymentUpdatedMessageTopic, subscriptionName);
        }

        public void Start()
        {
            var messageHandlerOptions = new MessageHandlerOptions(OnServiceBusException) { MaxConcurrentCalls = 4 };

            checkoutMessageReceiverClient.RegisterMessageHandler(OnCheckoutMessageReceived, messageHandlerOptions);
            orderPaymentUpdateMessageReceiverClient.RegisterMessageHandler(OnOrderPaymentUpdateReceived, messageHandlerOptions);
        }

        private async Task OnCheckoutMessageReceived(Message message, CancellationToken arg2)
        {
            var body = Encoding.UTF8.GetString(message.Body);//json from service bus

            //save order with status not paid
            BasketCheckoutMessage basketCheckoutMessage = JsonConvert.DeserializeObject<BasketCheckoutMessage>(body);

            Guid orderId = Guid.NewGuid();

            Order order = new Order
            {
                UserId = basketCheckoutMessage.UserId,
                Id = orderId,
                OrderPaid = false,
                OrderPlaced = DateTime.Now,
                OrderTotal = basketCheckoutMessage.BasketTotal
            };

            await _orderRepository.AddOrder(order);

            //send order payment request message
            OrderPaymentRequestMessage orderPaymentRequestMessage = new OrderPaymentRequestMessage
            {
                CardExpiration = basketCheckoutMessage.CardExpiration,
                CardName = basketCheckoutMessage.CardName,
                CardNumber = basketCheckoutMessage.CardNumber,
                OrderId = orderId,
                Total = basketCheckoutMessage.BasketTotal
            };

            try
            {
                await _messageBus.PublishMessage(orderPaymentRequestMessage, orderPaymentRequestMessageTopic);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task OnOrderPaymentUpdateReceived(Message message, CancellationToken arg2)
        {
            var body = Encoding.UTF8.GetString(message.Body);//json from service bus
            OrderPaymentUpdateMessage orderPaymentUpdateMessage =
                JsonConvert.DeserializeObject<OrderPaymentUpdateMessage>(body);

            await _orderRepository.UpdateOrderPaymentStatus(orderPaymentUpdateMessage.OrderId, orderPaymentUpdateMessage.PaymentSuccess);
        }

        private Task OnServiceBusException(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine(exceptionReceivedEventArgs);

            return Task.CompletedTask;
        }

        public void Stop()
        {
        }
    }
}
