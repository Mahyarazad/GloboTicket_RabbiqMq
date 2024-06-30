using GloboTicket.Integration.Messages;
using System.Threading.Tasks;

namespace GloboTicket.Integration.MessagingBus
{
    public interface IMessageBus
    {
        void  PublishMessage (IntegrationBaseMessage message, string topicName, string queueName, string routingkey);
    }
}
