using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Web;
using Api;
using Fleck;
using Microsoft.AspNetCore.Builder;
using WebSocketBoilerplate;

namespace ExerciseA;

public class CustomWebSocketServer(IConnectionManager manager)
{
    public void Start(WebApplication app)
    {
        var port = GetAvailablePort(8181);
        Environment.SetEnvironmentVariable("PORT", port.ToString());
        var url = $"ws://0.0.0.0:{port}";
        var server = new WebSocketServer(url);
        Action<IWebSocketConnection> config = ws =>
        {
            // Get just the query string part
            var queryString = ws.ConnectionInfo.Path.Split('?').Length > 1 
                ? ws.ConnectionInfo.Path.Split('?')[1] 
                : "";
        
            var query = HttpUtility.ParseQueryString(queryString);
            var id = query["id"];
            using var scope = app.Services.CreateScope();

            try
            {
                  ws.OnOpen = () => manager.OnOpen(ws, id);
                            ws.OnClose = () => manager.OnClose(ws, id);
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
          
            ws.OnError = e =>
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
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
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
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