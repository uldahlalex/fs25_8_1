using WebSocketBoilerplate;

namespace Api.EventHandlers.Dtos;

public class ServerAuthenticatesClientDto : BaseDto
{
    public List<string> Topics { get; set; } = new List<string>();
}