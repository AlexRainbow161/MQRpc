using Apache.NMS.AMQP;
using ArtemisServerTestApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQRpc.Artemis;
using MQRpc.Core.Interfaces;

#region configurations
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.local.json", optional: true)
    .Build();
#endregion

#region dependencyInjection
var services = new ServiceCollection();

var serverSettings = configuration.GetSection("ArtemisServerSettings").Get<ArtemisSettings>();

services.AddArtemisRpcServer<BaseCommand>(options =>
{
    options.RpcQueueName = serverSettings.RpcQueueName;
    options.Username = serverSettings.Username;
    options.Password = serverSettings.Password;
    options.Timeout = serverSettings.Timeout;
    options.BrokerUri = serverSettings.BrokerUri;
});

#endregion

//Build serviceProvider
var serviceProvider = services.BuildServiceProvider();

var server = serviceProvider.GetRequiredService<IRpcServer>();

Console.WriteLine("Server ready...");
Console.ReadKey();

using var mainProducerConnection = new ConnectionFactory(serverSettings.Username, serverSettings.Password, serverSettings.BrokerUri)
    .CreateConnection();

using var producerSession = mainProducerConnection.CreateSession();
using var producerDesination = producerSession.GetQueue(serverSettings.RpcQueueName);
using var producer = producerSession.CreateProducer(producerDesination);


var client = new ArtemisRpcClient(producer, serverSettings);

var command = new BaseCommand() { RequestMessage = "Hello?" };

try
{
    var result = await client.SendAsync(command);
    Console.WriteLine(result.Message);
}
catch(Exception ex)
{
    Console.WriteLine(ex.ToString());
}


Console.ReadKey();