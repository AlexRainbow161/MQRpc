using RabbitMQ.Client;

namespace MQRpc.RabbitMQ;

public class RabbitServerSettings
{
    public IAsyncConnectionFactory ConnectionFactory { get; set; } = new ConnectionFactory
    { HostName = "localhost", DispatchConsumersAsync = true };
    public string RpcQueueName { get; set; } = "rpc_queue";
    public string RpcExchangeName { get; set; } = "rpc_exchange";
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(100);
}