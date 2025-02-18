using WebSocketBoilerplate;

namespace Api.EventHandlers.Dtos;

public class ServerSendsErrorMessageDto : BaseDto
{
    public string Error { get; set; }
}