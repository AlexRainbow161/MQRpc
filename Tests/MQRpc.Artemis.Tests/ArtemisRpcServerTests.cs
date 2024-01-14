using Microsoft.Extensions.DependencyInjection;
using MQRpc.Artemis.IntegrationTests.Fixtures;
using MQRpc.Core.Interfaces;

namespace MQRpc.Artemis.IntegrationTests
{
    public class ArtemisRpcServerTests : IClassFixture<ServiceProviderFixrutre>
    {
        private readonly IServiceProvider _serviceProvider;

        public ArtemisRpcServerTests(ServiceProviderFixrutre serviceProviderFixrutre)
        {
            _serviceProvider = serviceProviderFixrutre.ServiceProvider;
        }

        [Fact]
        public void RunServerTest()
        {
            try
            {
                var service = _serviceProvider.GetRequiredService<IRpcServer>();
                Assert.True(true);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
            
        }
    }
}