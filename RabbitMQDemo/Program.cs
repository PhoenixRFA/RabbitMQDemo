using ExternalLogic;
using ExternalLogic.Models;
using RabbitMQ.Client;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EmailingOptions>(builder.Configuration.GetSection(nameof(EmailingOptions)));
builder.Services.Configure<SupportOptions>(builder.Configuration.GetSection(nameof(SupportOptions)));

// Add services to the container.
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

string rabbitmqConnectionString = builder.Configuration.GetConnectionString("Rabbitmq");
var rmqFactory = new ConnectionFactory
{
    HostName = rabbitmqConnectionString
};
IConnection connection = rmqFactory.CreateConnection();

builder.Services.AddSingleton<IConnection>(connection);

string redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var redisMultiplexer = ConnectionMultiplexer.Connect
(
    redisConnectionString
);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);

builder.Services.AddTransient<IEmailingService, EmailingService>();
builder.Services.AddTransient<ISupportService, SupportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapDefaultControllerRoute();
app.MapRazorPages();

app.Run();
