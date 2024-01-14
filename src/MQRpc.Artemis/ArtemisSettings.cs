using System;

namespace MQRpc.Artemis
{
    public class ArtemisSettings
    {
        public string BrokerUri { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string RpcQueueName { get; set; }
        public TimeSpan Timeout { get; set; }
    }
}
