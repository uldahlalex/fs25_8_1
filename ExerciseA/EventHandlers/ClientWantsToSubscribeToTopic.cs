using Fleck;
using WebSocketBoilerplate;

namespace ExerciseA.EventHandlers;

public class ClientWantsToSubscribeToTopicDto : BaseDto
{
    public string Topic { get; set; }
}

public class ClientWantsToSubscribeToTopicEventHandler : BaseEventHandler<ClientWantsToSubscribeToTopicDto> 
{
    public override Task Handle(ClientWantsToSubscribeToTopicDto dto, IWebSocketConnection socket)
    {
        
    }
}