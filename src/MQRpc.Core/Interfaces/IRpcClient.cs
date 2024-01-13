using System.Threading;
using System.Threading.Tasks;

namespace MQRpc.Core.Interfaces
{
    public interface IRpcClient
    {
        Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> command, CancellationToken cancellationToken = default);
    }
}