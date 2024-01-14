using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MQRpc.Artemis.IntegrationTests.Fixtures
{
    public class ServiceProviderFixrutre
    {
        public ServiceProviderFixrutre()
        {
            #region configurations
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.local.json", optional: true)
                .Build();
            #endregion

            #region dependencyInjection
            var services = new ServiceCollection();

            var serverSettings = Configuration.GetSection("ArtemisServerSettings").Get<ArtemisSettings>();

            services.AddArtemisRpcServer(options =>
            {
                options.RpcQueueName = serverSettings.RpcQueueName;
                options.Username = serverSettings.Username;
                options.Password = serverSettings.Password;
                options.Timeout = serverSettings.Timeout;
                options.BrokerUri = serverSettings.BrokerUri;
            });

            #endregion


            //Build serviceProvider
            ServiceProvider = services.BuildServiceProvider();
        }

        public IServiceProvider ServiceProvider { get; }
        public IConfiguration Configuration { get; }
    }
}
