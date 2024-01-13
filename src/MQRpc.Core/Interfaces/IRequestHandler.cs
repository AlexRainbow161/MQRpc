using System.Threading;
using System.Threading.Tasks;

namespace MQRpc.Core.Interfaces
{
    public interface IRequestHandler<in TRequest, TResponse> : IRequestHandler where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    public interface IRequestHandler
    {
        //Marker interface
    }
}