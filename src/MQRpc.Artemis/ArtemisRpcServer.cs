using MQRpc.Core;
using System;

namespace MQRpc.Artemis
{
    public class ArtemisRpcServer : RpcServerBase
    {
        public ArtemisRpcServer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}
