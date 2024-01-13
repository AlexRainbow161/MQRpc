using MQRpc.Core;
using MQRpc.Core.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using static MQRpc.Core.Utils.TypeUtils;

namespace MQRpc.RabbitMQ;

public sealed class RabbitRpcServer : RpcServerBase, IDisposable
{
    private readonly string _rpcExchangeName;
    private readonly string _rpcQueueName;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly TimeSpan _commandTimeout;


    public RabbitRpcServer(IAsyncConnectionFactory factory, Type assemblyMarker,
        IServiceProvider serviceProvider, string rpcExchangeName, string rpcQueueName,
        TimeSpan? commandTimeout = null) : base(serviceProvider)
    {
        factory.DispatchConsumersAsync = true;
        _rpcExchangeName = rpcExchangeName;
        _rpcQueueName = rpcQueueName;
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _commandTimeout = commandTimeout ?? TimeSpan.FromSeconds(100);

        ScanAsseblyForHandlers(assemblyMarker);
        Initialize();
        StartConsume();
    }

    private void Initialize()
    {
        _channel.ExchangeDeclare(_rpcExchangeName, "direct", true);

        _channel.QueueDeclare(
            queue: _rpcQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        foreach (var (service, _) in ServiceDefinitions)
        {
            var (request, _) = GetGenericServiceArguments(service);
            _channel.QueueBind(_rpcQueueName, _rpcExchangeName, request.Name);
        }



        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
    }

    private void StartConsume()
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += OnConsumerReceived;

        _channel.BasicConsume(queue: _rpcQueueName,
            autoAck: false,
            consumer: consumer);
    }

    private async Task OnConsumerReceived(object? sender, BasicDeliverEventArgs args)
    {
        var body = args.Body.ToArray();
        var props = args.BasicProperties;
        var replyProps = _channel.CreateBasicProperties();
        replyProps.CorrelationId = props.CorrelationId;
        var messageType = args.BasicProperties.Type;
        var messageRaw = Encoding.UTF8.GetString(body);
        object? response = null;

        try
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(_commandTimeout);
            response = await InvokeCommand(messageRaw, messageType, cts.Token);
        }
        catch (Exception e)
        {
            response = Activator.CreateInstance(typeof(Response<>).MakeGenericType(typeof(object)), e.InnerException ?? e);
        }
        finally
        {
            var json = JsonSerializer.Serialize(response);
            var responseBytes = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(exchange: string.Empty,
                routingKey: props.ReplyTo,
                basicProperties: replyProps,
                body: responseBytes);

            _channel.BasicAck(deliveryTag: args.DeliveryTag, multiple: false);
        }
    }

    public void Dispose()
    {
        if (_connection.IsOpen)
            _connection.Close();
    }
}