using System.Net;
using System.Net.Sockets;
using System.Reflection;
using ExerciseA;
using Fleck;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WebSocketBoilerplate;

public class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddOptionsWithValidateOnStart<AppOptions>()
            .Bind(builder.Configuration.GetSection(nameof(AppOptions)));
        var appOptions = builder.Services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<AppOptions>>()
            .CurrentValue;
        var redisConfig = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            Ssl = true,
            DefaultDatabase = 0,
            ConnectRetry = 5,
            ReconnectRetryPolicy = new ExponentialRetry(5000),
            EndPoints = { { appOptions.REDIS_HOST, 6379 } },
            User = appOptions.REDIS_USERNAME,
            Password = appOptions.REDIS_PASSWORD
        };

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var multiplexer = ConnectionMultiplexer.Connect(redisConfig);
            return multiplexer;
        });
        builder.Services.AddSingleton<ConnectionManager>();
        builder.Services.InjectEventHandlers(Assembly.GetExecutingAssembly()); 

        var app = builder.Build();

        var manager = app.Services.GetRequiredService<ConnectionManager>();

        var server = new WebSocketServer("ws://0.0.0.0:" + GetAvailablePort(8181));
        server.Start(socket =>
        {
            socket.OnOpen = async void () =>
            {
                try
                {
                    await manager.OnOpen(socket);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            };
            socket.OnClose = async void () =>
            {
                try
                {
                    await manager.OnClose(socket);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            };
            socket.OnMessage = async void (message) =>
            {
                try
                {
                    await app.CallEventHandler(socket, message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            };
        });

        app.Run();
    }


    private static int GetAvailablePort(int startPort)
    {
        var port = startPort;
        var isPortAvailable = false;

        do
        {
            try
            {
                var tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                tcpListener.Stop();
                isPortAvailable = true;
            }
            catch (SocketException)
            {
                port++;
            }
        } while (!isPortAvailable);

        Environment.GetEnvironmentVariable("PORT");
        return port;
    }
}