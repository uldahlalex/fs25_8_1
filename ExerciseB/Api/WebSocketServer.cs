using System.Net;
using System.Net.Sockets;
using System.Web;
using Api;
using Fleck;
using Microsoft.AspNetCore.Builder;
using WebSocketBoilerplate;

namespace ExerciseA;

public class CustomWebSocketServer(ConnectionManager manager)
{
    public void Start(WebApplication app)
    {
        var server = new WebSocketServer("ws://0.0.0.0:"+GetAvailablePort(8181));

        server.Start(socket =>
        {
            var uri = new Uri(socket.ConnectionInfo.Path, UriKind.RelativeOrAbsolute);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var id = query["id"];
            socket.OnOpen = async void() =>  await manager.OnOpen(socket, id);
            socket.OnClose = async void () => await manager.OnClose(socket, id);
            socket.OnMessage = async message =>
            {
                Task.Run(async () =>
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
                });
            };
        });
    }
    
    private int GetAvailablePort(int startPort)
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