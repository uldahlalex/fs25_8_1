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
            var fullUri = new Uri("ws://localhost" + socket.ConnectionInfo.Path);
            var query = HttpUtility.ParseQueryString(fullUri.Query);
            var id = query["id"];

            if (string.IsNullOrEmpty(id))
            {
                socket.Close();
                return;
            }
            socket.OnOpen =  () =>
            {
                Task.Run(async () => { await manager.OnOpen(socket, id); });
            };
            socket.OnClose =  () =>
            {
                Task.Run(async () =>
                {
                    await manager.OnClose(socket, id);

                });
            };
            socket.OnMessage = message =>
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