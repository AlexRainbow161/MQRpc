using System.Text;
using System.Text.Json;
using Apache.NMS.AMQP;
using Enva.Packages.Integrations.ArtemisRpc.Enums;
using Enva.Packages.Integrations.ArtemisRpc.Requests;
using Enva.Packages.Integrations.ArtemisRpc.RpcImplementation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQRpc.Artemis.TestApp;

#region configurations
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.local.json", optional: true)
    .Build();
#endregion

#region dependencyInjection
var services = new ServiceCollection();

var serverSettings = configuration.GetSection("ArtemisServerSettings").Get<ArtemisSettings>();

// services.AddArtemisRpcServer<BaseCommand>(options =>
// {
//     options.RpcQueueName = serverSettings.RpcQueueName;
//     options.Username = serverSettings.Username;
//     options.Password = serverSettings.Password;
//     options.Timeout = serverSettings.Timeout;
//     options.BrokerUri = serverSettings.BrokerUri;
// });

#endregion

//Build serviceProvider
var serviceProvider = services.BuildServiceProvider();

using var mainProducerConnection = new ConnectionFactory(serverSettings.Username, serverSettings.Password, serverSettings.BrokerUri.ToString())
    .CreateConnection();

using var producerSession = mainProducerConnection.CreateSession();
using var producerDesination = producerSession.GetQueue(serverSettings.RpcQueueName);
using var producer = producerSession.CreateProducer(producerDesination);


var client = new ArtemisRpcClient(producer, serverSettings);

var cmd = new BaseCommand()
{
    RequestMessage = "Hello, World!"
};

var cmdBytes = JsonSerializer.SerializeToUtf8Bytes(cmd);

var command =
    new InvokeHttpCommandRequest(new Uri("https://httpbin.org/post"), HttpVerb.Post, cmdBytes, new Dictionary<string, IEnumerable<string>>()
    {
        {"Accept", new List<string>(){"application/json"}}
    });

try
{
    var result = await client.SendAsync(command);
    var body = Encoding.UTF8.GetString(result.ResponseBody);
    Console.WriteLine(body);
}
catch(Exception ex)
{
    Console.WriteLine(ex.ToString());
}


Console.ReadKey();