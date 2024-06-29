namespace GloboTicket.Services.Ordering.Messaging
{
    public interface IRabbitMqConsumer
    {
        void Start();
        void Stop();
    }
}