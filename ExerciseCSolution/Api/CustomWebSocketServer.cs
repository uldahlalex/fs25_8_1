using System.Net;
using System.Net.Sockets;
using System.Web;
using Api.EventHandlers.Dtos;
using Fleck;
using WebSocketBoilerplate;

namespace Api;

public class CustomWebSocketServer(IConnectionManager manager, ILogger<CustomWebSocketServer> logger)
{
    public void Start(WebApplication app)
    {
        var port = GetAvailablePort(8181);
        Environment.SetEnvironmentVariable("PORT", port.ToString());
        var url = $"ws://0.0.0.0:{port}";
        var server = new WebSocketServer(url);
        
        server.Start(socket =>
        {
            var queryString = socket.ConnectionInfo.Path.Split('?').Length > 1
                ? socket.ConnectionInfo.Path.Split('?')[1]
                : "";

            var id = HttpUtility.ParseQueryString(queryString)["id"];

            socket.OnOpen = () => manager.OnOpen(socket, id);
            socket.OnClose = () => manager.OnClose(socket, id);
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
                        logger.LogError(e, "Error while handling message");
                        socket.SendDto(new ServerSendsErrorMessageDto()
                        {
                            Error = e.Message
                        });
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

        return port;
    }
}