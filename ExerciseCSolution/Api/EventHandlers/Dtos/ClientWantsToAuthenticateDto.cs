using WebSocketBoilerplate;

namespace Api.EventHandlers.Dtos;

public class ClientWantsToAuthenticateDto : BaseDto
{
    public string Username { get; set; }
}