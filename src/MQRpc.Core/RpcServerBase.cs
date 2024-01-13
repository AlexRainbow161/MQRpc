using Microsoft.Extensions.DependencyInjection;
using MQRpc.Core.Interfaces;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static MQRpc.Core.Utils.TypeUtils;

namespace MQRpc.Core
{
    /// <summary>
    /// That base class of RpcServer contains logic that can instantiate and invoke command handlers
    /// </summary>
    public abstract class RpcServerBase : IRpcServer
    {
        private readonly IServiceProvider _serviceProvider;

        public RpcServerBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected virtual async Task<object?> InvokeCommand(string message, string messageType, CancellationToken cancellationToken)
        {
            var serviceType = ResolveServiceType(messageType);

            using var scope = _serviceProvider.CreateScope();

            if (serviceType == null) throw new Exception($"Cannot resolve service type of {messageType}.");

            var service = scope.ServiceProvider.GetRequiredService(serviceType);

            var (requestType, responseType) = GetGenericServiceArguments(serviceType);

            object? response = null;

            var handlerMethod = serviceType.GetMethod("Handle");

            if (handlerMethod == null) throw new Exception($"Cannot resolve Handle method in service of type {messageType}");

            var typedMessage = JsonSerializer.Deserialize(message, requestType);

            await Task.Yield();

            dynamic? awaitable = handlerMethod.Invoke(service, new[] { typedMessage, cancellationToken });

            if (awaitable == null) return response;

            await awaitable.ConfigureAwait(false);

            var result = awaitable.GetAwaiter().GetResult();

            response = Activator.CreateInstance(typeof(Response<>)
                .MakeGenericType(responseType), new object[] { result });

            return response;
        }
    }
}
