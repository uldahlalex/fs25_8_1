using System.ComponentModel.DataAnnotations;
using WebSocketBoilerplate;

namespace Api.EventHandlers.Dtos;

public class ClientWantsToJoinRoomDto : BaseDto
{
    [MinLength(1)]
    public string RoomId { get; set; }   
}