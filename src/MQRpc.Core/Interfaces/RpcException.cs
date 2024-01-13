using System;

namespace MQRpc.Core.Interfaces
{
    public class RpcException : Exception
    {
        public RpcException(string message, string stackTrace) : base(message)
        {
            StackTrace = stackTrace;
        }

        public override string StackTrace { get; }
    }

    public class RpcExceptionWrapper
    {
        public RpcExceptionWrapper(string message, string? stackTrace)
        {
            Message = message;
            StackTrace = stackTrace;
        }

        public string Message { get; }
        public string StackTrace { get; }
    }
}