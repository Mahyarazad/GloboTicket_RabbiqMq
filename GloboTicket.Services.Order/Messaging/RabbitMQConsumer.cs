using GloboTicket.Services.Ordering.Entities;
using GloboTicket.Services.Ordering.Extensions;
using GloboTicket.Services.Ordering.Messages;
using GloboTicket.Services.Ordering.Repositories;


using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GloboTicket.Services.Ordering.Messaging
{
    public class RabbitMqConsumer: IRabbitMqConsumer
    {
        private readonly string subscriptionName = "globoticketorder";

        private readonly IConnection _connection;
        private readonly IModel orderPaymentUpdateMessageClient;
        private readonly IModel checkoutMessageClient;
        private readonly EventingBasicConsumer orderPaymentUpdateConsumer;
        private readonly EventingBasicConsumer checkoutMessageConsumer;
        private readonly IConfiguration _configuration;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly OrderRepository _orderRepository;

        private readonly string checkoutMessageTopic;
        private readonly string orderPaymentRequestMessageTopic;
        //private readonly string orderPaymentUpdatedMessageTopic;

        public RabbitMqConsumer(IConfiguration configuration, OrderRepository orderRepository)
        {
            _configuration = configuration;
            _orderRepository = orderRepository;


            var connectionFactory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMq:Host"],
                UserName = _configuration["RabbitMq:Username"],
                Password = _configuration["RabbitMq:Password"],
            };
            _cancellationTokenSource = new CancellationTokenSource();
            _connection = connectionFactory.CreateConnection();
            orderPaymentUpdateMessageClient = _connection.CreateModel();
            checkoutMessageClient = _connection.CreateModel();

            checkoutMessageClient.ExchangeDeclare(exchange: _configuration.GetValue<string>("Topic_Name"), type: ExchangeType.Topic);
            orderPaymentUpdateMessageClient.ExchangeDeclare(exchange: _configuration.GetValue<string>("Topic_Name"), type: ExchangeType.Topic);


            checkoutMessageTopic = _configuration.GetValue<string>("CheckoutMessageTopic");
            orderPaymentRequestMessageTopic = _configuration.GetValue<string>("OrderPaymentRequestMessageTopic");

            checkoutMessageClient.QueueDeclare(checkoutMessageTopic, true, false, false, null);
            checkoutMessageClient.QueueBind(checkoutMessageTopic, _configuration.GetValue<string>("Topic_Name"), "payment.*");
            orderPaymentUpdateMessageClient.QueueDeclare(orderPaymentRequestMessageTopic, true, false,false , null);
            orderPaymentUpdateMessageClient.QueueBind(orderPaymentRequestMessageTopic, _configuration.GetValue<string>("Topic_Name"), "payment.order");

            checkoutMessageConsumer = new EventingBasicConsumer(checkoutMessageClient);
            orderPaymentUpdateConsumer = new EventingBasicConsumer(orderPaymentUpdateMessageClient);


        }

        public void Start()
        {

            checkoutMessageConsumer.Received += async (sender, arg) =>
            {
                var message = Serializer<BasketCheckoutMessage>.Deserialize(arg.Body.ToArray());
                await OnCheckoutMessageReceived(message, _cancellationTokenSource.Token);
            };

            checkoutMessageClient.BasicConsume(checkoutMessageTopic, true, checkoutMessageConsumer);

            orderPaymentUpdateConsumer.Received += async (sender, arg) =>
            {
                var message = Serializer<OrderPaymentUpdateMessage>.Deserialize(arg.Body.ToArray());
                await OnOrderPaymentUpdateReceived(message, _cancellationTokenSource.Token);
            };

            orderPaymentUpdateMessageClient.BasicConsume(orderPaymentRequestMessageTopic, true, orderPaymentUpdateConsumer);

        }

        private async Task OnCheckoutMessageReceived(BasketCheckoutMessage basketCheckoutMessage, CancellationToken arg2)
        {

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
                checkoutMessageClient.BasicPublish(_configuration.GetValue<string>("Topic_Name"), checkoutMessageTopic, null
                    , Serializer<OrderPaymentRequestMessage>.Serialize(orderPaymentRequestMessage));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task OnOrderPaymentUpdateReceived(OrderPaymentUpdateMessage message, CancellationToken arg2)
        {
            await _orderRepository.UpdateOrderPaymentStatus(message.OrderId, message.PaymentSuccess);
        }

        public void Stop()
        {
        }
    }
}
