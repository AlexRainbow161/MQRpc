using Microsoft.Extensions.DependencyInjection;
using static MQRpc.Core.Utils.TypeUtils;

namespace MQRpc.RabbitMQ;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddRabbitRpcClient(this IServiceCollection serviceCollection,
        Action<RabbitClientSettings>? options = null)
    {
        var settins = new RabbitClientSettings();
        options?.Invoke(settins);

        serviceCollection.AddSingleton(_ => new RabbitRpcClient(settins.ConnectionFactory, settins.RpcExchangeName));

        return serviceCollection;
    }

    public static IServiceCollection AddRabbitRpcServer<TMarker>(this IServiceCollection serviceCollection,
        Action<RabbitServerSettings>? options = null)
    {
        var settings = new RabbitServerSettings();
        options?.Invoke(settings);

        var assemblyMarker = typeof(TMarker);

        ScanAsseblyForHandlers(assemblyMarker);

        foreach (var (service, implementation) in ServiceDefinitions)
        {
            serviceCollection.AddScoped(service, implementation);
        }

        serviceCollection.AddSingleton(sp => new RabbitRpcServer(settings.ConnectionFactory,
            assemblyMarker,
            sp,
            settings.RpcExchangeName,
            settings.RpcQueueName,
            settings.CommandTimeout));

        return serviceCollection;
    }
}