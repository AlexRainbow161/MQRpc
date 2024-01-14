using Apache.NMS;
using Apache.NMS.AMQP;
using MQRpc.Artemis.Exceptions;
using MQRpc.Core;
using MQRpc.Core.Interfaces;
using System;
using System.Text.Json;
using System.Threading;

namespace MQRpc.Artemis
{
    public class ArtemisRpcServer : RpcServerBase, IDisposable
    {
        private readonly ArtemisSettings _serverSettings;
        private IConnection _connection;
        private ISession _session;
        private IMessageConsumer _consumer;
        private IMessageProducer _producer;

        public ArtemisRpcServer(IServiceProvider serviceProvider, ArtemisSettings serverSettings) : base(serviceProvider)
        {
            _serverSettings = serverSettings;
            _connection = CreateConnection();
            _session = _connection.CreateSession();
            _consumer = CreateConsumer();
            _producer = CreateProducer();
            StartConsume();
        }

        public void Dispose()
        {
            _consumer.Dispose();
            _producer.Dispose();
            _session.Dispose();
            _connection.Dispose();
        }

        private IConnection CreateConnection()
        {
            var factory = new ConnectionFactory(_serverSettings.Username, _serverSettings.Password, _serverSettings.BrokerUri);
            return factory.CreateConnection();
        }

        private IMessageConsumer CreateConsumer()
        {
            var inputQueue = _session.GetQueue(_serverSettings.RpcQueueName);
            var consumer = _session.CreateConsumer(inputQueue);
            consumer.Listener += OnMessage;
            return consumer;
        }

        private IMessageProducer CreateProducer()
        {
            return _session.CreateProducer();
        }

        private void Send(object response, IMessage inputMessage)
        {
            var json = JsonSerializer.Serialize(response);
            ITextMessage responseMessage = _session.CreateTextMessage(json);
            responseMessage.NMSCorrelationID = inputMessage.NMSCorrelationID;
            _producer.Send(inputMessage.NMSReplyTo, responseMessage);
        }

        private void StartConsume()
        {
            _connection.Start();
        }

        private void OnMessage(IMessage message)
        {
            object response = null;

            bool sendCallback = true;

            try
            {
                if (message.NMSReplyTo is null)
                {
                    throw new InternalServerException("ReplyTo not set");
                }
                if (string.IsNullOrEmpty(message.NMSCorrelationID))
                {
                    throw new InternalServerException("CorrelationId not set");
                }
                if (message is not ITextMessage textMessage)
                {
                    throw new InternalServerException("Is not a text message");
                }

                using var cts = new CancellationTokenSource(_serverSettings.Timeout);
                response = InvokeCommand(textMessage.Text, textMessage.NMSType, cts.Token).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                if (e is InternalServerException)
                {
                    sendCallback = false;
                    return;
                }
                response = Activator.CreateInstance(typeof(Response<>).MakeGenericType(typeof(object)), e.InnerException ?? e);
            }
            finally
            {
                if (sendCallback) Send(response, message);
                message.Acknowledge();
            }
        }
    }
}
