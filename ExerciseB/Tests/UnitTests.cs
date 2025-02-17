using System.Text.Json;
using Api;
using Fleck;
using Moq;

namespace Tests;

public class UnitTests
{
    private readonly DictionaryConnectionManager _manager;

    public UnitTests()
    {
        _manager = new DictionaryConnectionManager();
    }

    [Fact]
    public async Task OnConnect_Can_Add_Socket_And_Client_To_Dictionaries()
    {
        //arrange
        var connectionId = Guid.NewGuid().ToString();
        var socketId = Guid.NewGuid();
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(ws => ws.ConnectionInfo.Id).Returns(socketId);
        var ws = wsMock.Object;
        //act
        await _manager.OnOpen(ws, connectionId);
        
        //assert
        Assert.Equal(_manager.Sockets.Values.First(), ws);
        if (!_manager.TopicMembers[DictionaryConnectionManager.TopicSocketsKey].Contains(connectionId))
            throw new Exception("Expected client id "+connectionId+" to be in hash set(value) of key "+DictionaryConnectionManager.TopicSocketsKey+" " +
                                "Values found in dictionary: "+JsonSerializer.Serialize(_manager.TopicMembers));
    }
}