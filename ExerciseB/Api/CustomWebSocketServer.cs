using System.Net;
using System.Net.Sockets;
using System.Text.Json;
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
        var port = GetAvailablePort(8181);
        Environment.SetEnvironmentVariable("PORT", port.ToString());
        var url = $"ws://0.0.0.0:{port}";
        var server = new WebSocketServer(url);
        Action<IWebSocketConnection> config = ws =>
        {
            var fulluri = url + ws.ConnectionInfo.Path;
            var query = HttpUtility.ParseQueryString(fulluri);
            var id = query["id"];

            using var scope = app.Services.CreateScope();
            var wsService = scope.ServiceProvider.GetRequiredService<ConnectionManager>();

            ws.OnOpen = () => wsService.OnOpen(ws, id);
            ws.OnClose = () => wsService.OnClose(ws, id);
            ws.OnError = ex =>
            {
           
                ws.Send(JsonSerializer.Serialize(new BaseDto()));
            };
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
                        var baseDto = JsonSerializer.Deserialize<BaseDto>(message);
                        ws.SendDto(baseDto);
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