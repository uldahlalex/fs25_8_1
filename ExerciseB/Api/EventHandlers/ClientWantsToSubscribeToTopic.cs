using Api;
using Fleck;
using WebSocketBoilerplate;

namespace ExerciseA.EventHandlers;

public class ClientWantsToSubscribeToTopicDto : BaseDto
{
    public string Topic { get; set; }
}

public class ServerHasAddedConnectionToTopicSubscription : BaseDto
{
    
}

public class ClientWantsToSubscribeToTopicEventHandler(ConnectionManager manager) : BaseEventHandler<ClientWantsToSubscribeToTopicDto> 
{
    public override async Task Handle(ClientWantsToSubscribeToTopicDto dto, IWebSocketConnection socket)
    {
        var theGeneratedClientId = manager.LookupByKey(ConnectionManager.TOPIC_SOCKET_ID(socket.ConnectionInfo.Id.ToString()));
        await manager.Subscribe(theGeneratedClientId, dto.Topic);
    }
}