using System.Net;
using System.Net.Sockets;
using Fleck;
using Microsoft.AspNetCore.Builder;

namespace ExerciseA;

public class CustomWebSocketServer(
    Func<IWebSocketConnection, Task> onOpen,  
    Func<IWebSocketConnection, Task> onClose)
{
    public void Start(WebApplication app)
    {
        var server = new WebSocketServer("ws://0.0.0.0:"+GetAvailablePort(8181));

        server.Start(socket =>
        {
            socket.OnOpen = async void() =>  await onOpen(socket);
            socket.OnClose = async void () => await onClose(socket);
            socket.OnMessage = async message => { };
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