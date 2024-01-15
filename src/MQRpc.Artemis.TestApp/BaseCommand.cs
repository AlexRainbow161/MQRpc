using MQRpc.Core.Interfaces;

namespace MQRpc.Artemis.TestApp
{
    public class BaseCommand : IRequest<BaseResponse>
    {
        public string RequestMessage { get; set; }
    }

    public class BaseResponse
    {
        public string Message { get; set; }
    }

    public class Handler : IRequestHandler<BaseCommand, BaseResponse>
    {
        public Task<BaseResponse> Handle(BaseCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult<BaseResponse>(new BaseResponse
            {
                Message = request.RequestMessage + " Answered!"
            });
        }
    }
}
