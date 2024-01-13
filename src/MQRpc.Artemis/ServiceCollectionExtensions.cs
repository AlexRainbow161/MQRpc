using Microsoft.Extensions.DependencyInjection;
using System;

namespace MQRpc.Artemis
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddArtemisRpcServer(this IServiceCollection services, Action<ArtemisServerSettings> settingsBuilder = null)
        {
            var settings = new ArtemisServerSettings();
            settingsBuilder?.Invoke(settings);
            return services;
        }

        public static IServiceCollection AddArtemisRpcClient(this IServiceCollection services, Action<ArtemisClientSettings> settingsBuilder = null)
        {
            var settings = new ArtemisClientSettings();
            settingsBuilder?.Invoke(settings);
            return services;
        }
    }
}
