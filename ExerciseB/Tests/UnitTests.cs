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
        var topic = "sockets";
        
        //act
        await _manager.OnOpen(ws, connectionId);
        
        //assert
        Assert.Equal(_manager.Sockets.Values.First(), ws);
        if (!_manager.TopicMembers["sockets"].Contains(connectionId))
            throw new Exception("Expected client id "+connectionId+" to be in hash set(value) of key 'sockets' " +
                                "Values found in dictionary: "+JsonSerializer.Serialize(_manager.TopicMembers));
        if(!_manager.MemberTopics[connectionId].Contains("sockets"))
            throw new Exception("Expected topic "+topic+" to be in hash set(value) of key 'sockets' " +
                                "Values found in dictionary: "+JsonSerializer.Serialize(_manager.MemberTopics));
    }
    
    [Fact]
    public async Task OnClose_Can_Remove_Socket_And_Client_From_Dictionaries()
    {
        //arrange
        var connectionId = Guid.NewGuid().ToString();
        var socketId = Guid.NewGuid();
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(ws => ws.ConnectionInfo.Id).Returns(socketId);
        var ws = wsMock.Object;
        var topic = "sockets";
        await _manager.OnOpen(ws, connectionId);
        
        //act
        await _manager.OnClose(ws, connectionId);
        
        //assert
        Assert.DoesNotContain(_manager.Sockets.Values, s => s.ConnectionInfo.Id == socketId);
        if (_manager.TopicMembers["sockets"].Contains(connectionId))
            throw new Exception("Expected client id "+connectionId+" to not be in hash set(value) of key 'sockets' " +
                                "Values found in dictionary: "+JsonSerializer.Serialize(_manager.TopicMembers));
        if (_manager.MemberTopics.Keys.Contains(connectionId))
            throw new Exception("Expected memberTopics to not have key "+connectionId+" " +
                                "Keys found in dictionary: "+JsonSerializer.Serialize(_manager.MemberTopics));
        
    }
}