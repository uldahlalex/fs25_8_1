using System.Reflection;
using Microsoft.Extensions.Options;
using WebSocketBoilerplate;

namespace Api;

public class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddOptionsWithValidateOnStart<AppOptions>()
            .Bind(builder.Configuration.GetSection(nameof(AppOptions)));
        builder.Services.AddSingleton<IConnectionManager, DictionaryConnectionManager>();
        builder.Services.AddSingleton<CustomWebSocketServer>();
        builder.Services.InjectEventHandlers(Assembly.GetExecutingAssembly());
        var app = builder.Build();
        app.Services.GetRequiredService<CustomWebSocketServer>().Start(app);
        app.Run();
    }
}