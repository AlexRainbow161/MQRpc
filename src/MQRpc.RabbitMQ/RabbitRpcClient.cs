using MQRpc.Core.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace MQRpc.RabbitMQ;

public sealed class RabbitRpcClient : IDisposable, IRpcClient
{
    private readonly string _rpcExcangeName;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private string? _queue;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new();

    public RabbitRpcClient(IConnectionFactory connectionFactory, string rpcExcangeName)
    {
        _rpcExcangeName = rpcExcangeName;
        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        Initialize();
        StartConsume();
    }

    private void Initialize()
    {
        _queue = _channel.QueueDeclare().QueueName;
    }

    private void StartConsume()
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += OnConsumerReceived;
        _channel.BasicConsume(queue: _queue,
            autoAck: true,
            consumer: consumer);
    }

    private void OnConsumerReceived(object? sender, BasicDeliverEventArgs args)
    {
        var correlationId = args.BasicProperties.CorrelationId;
        if (!_callbackMapper.TryRemove(correlationId, out var taskCompletionSource)) return;
        var body = args.Body.ToArray();
        var response = Encoding.UTF8.GetString(body);
        taskCompletionSource.TrySetResult(response);
    }

    public Task<TResponse?> SendAsync<TResponse>(IRequest<TResponse> command, CancellationToken cancellationToken = default)
    {
        var props = _channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = _queue;
        var commandType = command.GetType();
        var commandTypeName = commandType.FullName;
        var commandTypeShortName = commandType.Name;
        props.Type = string.Concat(commandTypeName.AsSpan(), "^", typeof(TResponse).FullName.AsSpan());
        var messageBytes = ToUtf8Bytes(command);
        var taskCompletionSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _callbackMapper.TryAdd(correlationId, taskCompletionSource);
        _channel.BasicPublish(exchange: _rpcExcangeName,
            routingKey: commandTypeShortName,
            basicProperties: props,
            body: messageBytes);

        cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out _));

        return taskCompletionSource.Task
            .ContinueWith(task =>
            {
                var responseResult = JsonSerializer.Deserialize<Response<TResponse>>(task.Result);
                if (responseResult is null) throw new Exception("RPC Server return null response.");
                if (responseResult.Exception is not null)
                    throw new RpcException(responseResult.Exception.Message, responseResult.Exception.StackTrace);
                return responseResult.Value ?? default;
            }, cancellationToken);
    }

    private static byte[] ToUtf8Bytes(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return Encoding.UTF8.GetBytes(json);
    }

    public void Dispose()
    {
        _connection.Close();
    }
}