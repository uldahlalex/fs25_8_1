using System.Text.Json;
using Api;
using Fleck;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests;

public class ConnectionManagerTests
{
    private readonly DictionaryConnectionManager _dictionaryManager;
    private readonly AppOptions _appOptions;

    public ConnectionManagerTests()
    {
        var loggerFactory = new LoggerFactory();
        var dictionaryLogger = new Logger<DictionaryConnectionManager>(loggerFactory);
        _dictionaryManager = new DictionaryConnectionManager(dictionaryLogger);
    }

    [Fact]
    public async Task OnConnect_Can_Add_Socket_And_Client_To_Storage()
    {
        // arrange
        var manager = _dictionaryManager;

        var connectionId = Guid.NewGuid().ToString();
        var socketId = Guid.NewGuid();
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(ws => ws.ConnectionInfo.Id).Returns(socketId);
        var ws = wsMock.Object;

        // act
        await manager.OnOpen(ws, connectionId);

        // assert
        Assert.Equal(manager.ConnectionIdToSocket.Values.First(), ws);
        Assert.Equal(manager.SocketToConnectionId.Values.First(), connectionId);
    }

    [Fact]
    public async Task OnClose_Can_Remove_Socket_And_Client_From_Storage()
    {
        // arrange
        var manager = _dictionaryManager;

        var connectionId = Guid.NewGuid().ToString();
        var socketId = Guid.NewGuid();
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(ws => ws.ConnectionInfo.Id).Returns(socketId);
        var ws = wsMock.Object;
        await manager.OnOpen(ws, connectionId);

        // act
        await manager.OnClose(ws, connectionId);

        // assert
        Assert.DoesNotContain(manager.ConnectionIdToSocket.Values, s => s.ConnectionInfo.Id == socketId);
        Assert.DoesNotContain(manager.SocketToConnectionId.Values, c => c == connectionId);
    }


}