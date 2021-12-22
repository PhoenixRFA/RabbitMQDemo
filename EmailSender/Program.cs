using EmailSender;
using EmailSender.Services;
using ExternalLogic;
using ExternalLogic.Models;
using RabbitMQ.Client;
using StackExchange.Redis;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<EmailingOptions>(ctx.Configuration.GetSection(nameof(EmailingOptions)));
        services.Configure<SupportOptions>(ctx.Configuration.GetSection(nameof(SupportOptions)));

        services.AddHostedService<Worker>();

        string rabbitmqConnectionString = ctx.Configuration.GetConnectionString("Rabbitmq");
        ConnectionFactory rmqFactory = new ConnectionFactory
        {
            HostName = rabbitmqConnectionString
        };

        IConnection rmqConnection = rmqFactory.CreateConnection();
        services.AddSingleton(rmqConnection);

        string redisConnectionString = ctx.Configuration.GetConnectionString("Redis");
        var redisMultiplexer = ConnectionMultiplexer.Connect
        (
            redisConnectionString
        );
        services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);

        services.AddTransient<IEmailingService, DemoEmailingService>();
    })
    .Build();

await host.RunAsync();
