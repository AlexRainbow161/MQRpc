using Microsoft.Extensions.DependencyInjection;
using MQRpc.Core.Interfaces;
using System;
using static MQRpc.Core.Utils.TypeUtils;

namespace MQRpc.Artemis
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddArtemisRpcServer<TMarker>(this IServiceCollection services, Action<ArtemisSettings> settingsBuilder = null)
        {
            var settings = new ArtemisSettings();
            settingsBuilder?.Invoke(settings);

            var assemblyMarker = typeof(TMarker);

            ScanAsseblyForHandlers(assemblyMarker);

            foreach (var (service, implementation) in ServiceDefinitions)
            {
                services.AddScoped(service, implementation);
            }

            services.AddSingleton<IRpcServer>(sp => new ArtemisRpcServer(sp, settings));
            return services;
        }

        public static IServiceCollection AddArtemisRpcClient(this IServiceCollection services, Action<ArtemisSettings> settingsBuilder = null)
        {
            var settings = new ArtemisSettings();
            settingsBuilder?.Invoke(settings);
            //services.AddSingleton<IRpcClient>(sp => new ArtemisRpcClient());
            return services;
        }
    }
}
