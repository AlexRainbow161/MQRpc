using RabbitMQ.Client;

namespace MQRpc.RabbitMQ;

public class RabbitClientSettings
{
    public IConnectionFactory ConnectionFactory { get; set; } = new ConnectionFactory
    { HostName = "localhost" };
    public string RpcExchangeName { get; set; } = "rpc_exchange";
}