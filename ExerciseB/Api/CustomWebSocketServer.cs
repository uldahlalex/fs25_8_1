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
        
        Action<IWebSocketConnection> config = ws =>
        {
            var queryString = ws.ConnectionInfo.Path.Split('?').Length > 1
                ? ws.ConnectionInfo.Path.Split('?')[1]
                : "";

            var id = HttpUtility.ParseQueryString(queryString)["id"];

            ws.OnOpen = () => manager.OnOpen(ws, id);
            ws.OnClose = () => manager.OnClose(ws, id);
            ws.OnMessage = message =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await app.CallEventHandler(ws, message);
                    }
                    catch (Exception e)
                    { 
                        logger.LogError(e, "Error while handling message");
                        ws.SendDto(new ServerSendsErrorMessageDto()
                        {
                            Error = e.Message
                        });
                    }
                });
            };
        };
        server.Start(config);
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