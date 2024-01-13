using MQRpc.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQRpc.Artemis
{
    public class ArtemisRpcClient : IRpcClient
    {
        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> command, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
