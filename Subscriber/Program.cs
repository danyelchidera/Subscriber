

// Setup Host
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var host = CreateDefaultBuilder().Build();

// Invoke Worker
using IServiceScope serviceScope = host.Services.CreateScope();
IServiceProvider provider = serviceScope.ServiceProvider;
var workerInstance = provider.GetRequiredService<Worker>();
workerInstance.DoWork();

host.Run();


static IHostBuilder CreateDefaultBuilder()
{
    return Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(app =>
        {
            app.AddJsonFile("appsettings.json");
        })
        .ConfigureServices(services =>
        {
            services.AddSingleton<Worker>();
        });
}

// Worker.cs
internal class Worker
{
    private readonly IConfiguration configuration;
    private IConnection _connection;
    private IModel _channel;

    public Worker(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void DoWork()
    {
        var uri = configuration["rabbitMqConnectionString"];
        var connectionFactory = new ConnectionFactory();
        connectionFactory.Uri = new Uri(uri);
        connectionFactory.AutomaticRecoveryEnabled = true;
        connectionFactory.DispatchConsumersAsync = true;
        _connection = connectionFactory.CreateConnection("rabbitMqConnn");

       _channel = _connection.CreateModel();
        _channel.BasicQos(0, 1, false);
        var emailChannelConsumer = new AsyncEventingBasicConsumer(_channel);
        emailChannelConsumer.Received += RecieveEmail;
        _channel.BasicConsume("Email", false, emailChannelConsumer);
    }

    private Task RecieveEmail(object sender, BasicDeliverEventArgs e)
    {
        var message = Encoding.UTF8.GetString(e.Body.ToArray());
        Console.WriteLine(message);

        _channel.BasicAck(e.DeliveryTag, false);

        return Task.CompletedTask;  
    }
}