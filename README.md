# RabbitMQ RPC ClientServer Library

## Usage Server
Add following code to Program.cs
```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        //Тут единственный супер критичный момент. Надо выбрать
        //тип который содержится в том ассебли где лежат хендлеры
        //Потом найду способ обойти это ограничение.
        //С другой стороны почти все подобные либы имеют это ограничение
        services.AddRabbitRpcServer<WeatherForecast>(); 
        services.AddHostedService<Worker>();
    })
    .Build();

using var server = host.Services.GetService<RpcServer>();

await host.RunAsync();
```

also you can specify your own connection settings and queue name.
Simply use options delegate of extension method

```csharp
services.AddRabbitRpcServer<WeatherForecast>(options =>
        {
            options.ConnectionFactory = new ConnectionFactory
            {
                HostName = "your.amqp.server",
                //other settings...
            };
            options.RpcQueueName = "Your rpc queue name";
            options.CommandTimeout = TimeSpan.FromSeconds(100);
        });
```

Then you have to declare Requests and RequestHandler

```csharp
//Request
public record ForecastRequest() : IRequest<IEnumerable<WeatherForecast>>;
```

```csharp
//Handler
public class ForecastRequestHandler : IRequestHandler<ForecastRequest, IEnumerable<WeatherForecast>>
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };
    
    public Task<IEnumerable<WeatherForecast>> Handle(ForecastRequest request, CancellationToken cancellationToken = default)
    {
        var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray().AsEnumerable();
        
        return Task.FromResult(forecast);
    }
}
```
Default life cycle of handlers is scoped, 
but latter i'll add method to change life cycle on what you want while configuration

Now you can handle requests on server side.

## Usage Client
Add following code to Program.cs in your client side project, web api for example

```csharp
builder.Services.AddRabbitRpcClient();
```

or with your custom settings

```csharp
builder.Services.AddRabbitRpcClient(options =>
        {
            options.ConnectionFactory = new ConnectionFactory
            {
                HostName = "your.amqp.server",
                //other settings...
            };
            options.RpcQueueName = "Your rpc queue name";
        });
```

but keep in mind, you have to use same rabbit server and rpc queue name
for both client and server. 

And that's it. Use client as dependency of any service what you want.

Example of service:
```csharp
public class WeaterForecastService : IWeaterForecastService
{
    private readonly RpcClient _rpcClient;

    public WeaterForecastService(RpcClient rpcClient)
    {
        _rpcClient = rpcClient;
    }

    public Task<IEnumerable<WeatherForecast>?> GetForecast()
    {
        return _rpcClient.SendAsync(new ForecastRequest());
    }
}
```

You can find all examples code in Examples folder at this repo. 