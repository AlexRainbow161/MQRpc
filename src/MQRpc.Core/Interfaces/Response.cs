using System;
using System.Text.Json.Serialization;

namespace MQRpc.Core.Interfaces
{
    public sealed class Response<TResponse>
    {
        public Response(TResponse value)
        {
            Value = value;
            Exception = null;
        }

        public Response(Exception? exception)
        {
            Value = default!;
            Exception = exception != null ? new RpcExceptionWrapper(exception.Message, exception.StackTrace) : null;
        }

        public Response(TResponse value, Exception? exception = null)
        {
            Value = value;
            Exception = exception != null ? new RpcExceptionWrapper(exception.Message, exception.StackTrace) : null;
        }

        [JsonConstructor]
        public Response(TResponse value, RpcExceptionWrapper? exception)
        {
            Value = value;
            Exception = exception;
        }

        public static implicit operator Response<TResponse>(TResponse response) => new(response);

        public TResponse Value { get; }
        public RpcExceptionWrapper? Exception { get; }
    }
}