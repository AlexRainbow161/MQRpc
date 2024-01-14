using Apache.NMS;
using Apache.NMS.AMQP;
using MQRpc.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MQRpc.Artemis
{
    public class ArtemisRpcClient : IRpcClient, IDisposable
    {
        private readonly IMessageProducer _producer;
        private readonly IMessageConsumer _consumer;
        private readonly ArtemisSettings _settings;
        private readonly IConnection _connection;
        private readonly ISession _session;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
        private ITemporaryQueue _temporaryQueue;

        public ArtemisRpcClient(IMessageProducer producer, ArtemisSettings settings)
        {
            _producer = producer;
            _settings = settings;
            _connection = CreateConsumerConnection();
            _session = CreateConsumerSession();
            _consumer = CreateReplyQueueConsumer();
            StartConsume();
        }

        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> command, CancellationToken cancellationToken = default)
        {
            var commandType = command.GetType();
            var messageBody = JsonSerializer.Serialize(command, commandType);
            var message = _session.CreateTextMessage(messageBody);
            message.NMSCorrelationID = Guid.NewGuid().ToString();
            message.NMSReplyTo = _temporaryQueue;
            var commandTypeName = commandType.FullName;
            message.NMSType = string.Concat(commandTypeName.AsSpan(), "^", typeof(TResponse).FullName.AsSpan());
            
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _callbackMapper.TryAdd(message.NMSCorrelationID, tcs);
            
            cancellationToken.Register(() => _callbackMapper.TryRemove(message.NMSCorrelationID, out _));

            _producer.Send(message);

            return tcs.Task
            .ContinueWith(task =>
            {
                var responseResult = JsonSerializer.Deserialize<Response<TResponse>>(task.Result);
                if (responseResult is null) throw new Exception("RPC Server return null response.");
                if (responseResult.Exception is not null)
                    throw new RpcException(responseResult.Exception.Message, responseResult.Exception.StackTrace);
                return responseResult.Value ?? default;
            }, cancellationToken);
        }

        public void Dispose()
        {
            _consumer.Dispose();
            _session.Dispose();
            _connection.Dispose();
        }

        private void StartConsume()
        {
            _connection.Start();
        }

        private IConnection CreateConsumerConnection()
        {
            var connectionFactory = new ConnectionFactory(_settings.Username, _settings.Password, _settings.BrokerUri);
            return connectionFactory.CreateConnection();
        }

        private ISession CreateConsumerSession()
        {
            return _connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
        }

        private IMessageConsumer CreateReplyQueueConsumer()
        {
            _temporaryQueue = _session.CreateTemporaryQueue();
            var consumer = _session.CreateConsumer(_temporaryQueue);
            consumer.Listener += OnMessage;
            return consumer;
        }

        private void OnMessage(IMessage message)
        {
            if (!_callbackMapper.TryRemove(message.NMSCorrelationID, out var tcs)) return;
            var responseMessage = (message as ITextMessage)?.Text;
            tcs.TrySetResult(responseMessage);
        }
    }
}
