using NetSdrClientApp.Networking;
using System.Net;
using System.Net.Sockets;

namespace NetSdrClientAppTests;

public class TcpClientWrapperTests
{
    private TcpListener _server = null!;
    private int _port;

    [SetUp]
    public void SetUp()
    {
        _server = new TcpListener(IPAddress.Loopback, 0);
        _server.Start();
        _port = ((IPEndPoint)_server.LocalEndpoint).Port;
    }

    [TearDown]
    public void TearDown()
    {
        _server.Stop();
        _server.Dispose();
    }

    [Test]
    public void Connected_BeforeConnect_ReturnsFalse()
    {
        var client = new TcpClientWrapper("127.0.0.1", _port);

        Assert.That(client.Connected, Is.False);
    }

    [Test]
    public void Connected_AfterSuccessfulConnect_ReturnsTrue()
    {
        var client = new TcpClientWrapper("127.0.0.1", _port);
        _ = _server.AcceptTcpClientAsync();

        client.Connect();

        Assert.That(client.Connected, Is.True);

        client.Disconnect();
    }

    [Test]
    public void Connected_AfterDisconnect_ReturnsFalse()
    {
        var client = new TcpClientWrapper("127.0.0.1", _port);
        _ = _server.AcceptTcpClientAsync();

        client.Connect();
        client.Disconnect();

        Assert.That(client.Connected, Is.False);
    }

    [Test]
    public void Connect_WhenAlreadyConnected_DoesNotThrowAndRemainsConnected()
    {
        var client = new TcpClientWrapper("127.0.0.1", _port);
        _ = _server.AcceptTcpClientAsync();

        client.Connect();

        Assert.DoesNotThrow(client.Connect);
        Assert.That(client.Connected, Is.True);

        client.Disconnect();
    }

    [Test]
    public void Connect_WhenServerUnreachable_DoesNotThrowAndRemainsDisconnected()
    {
        var client = new TcpClientWrapper("127.0.0.1", 1);

        Assert.DoesNotThrow(client.Connect);
        Assert.That(client.Connected, Is.False);
    }

    [Test]
    public void Disconnect_WhenNotConnected_DoesNotThrow()
    {
        var client = new TcpClientWrapper("127.0.0.1", _port);

        Assert.DoesNotThrow(client.Disconnect);
    }

    [Test]
    public void Disconnect_WhenConnected_ClosesConnection()
    {
        var client = new TcpClientWrapper("127.0.0.1", _port);
        _ = _server.AcceptTcpClientAsync();

        client.Connect();
        client.Disconnect();

        Assert.That(client.Connected, Is.False);
    }

    [Test]
    public void SendMessageAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var client = new TcpClientWrapper("127.0.0.1", _port);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SendMessageAsync([0x01, 0x02]));
    }

    [Test]
    public async Task SendMessageAsync_WhenConnected_DataArrivesOnServer()
    {
        var client = new TcpClientWrapper("127.0.0.1", _port);
        var serverAcceptTask = _server.AcceptTcpClientAsync();

        client.Connect();

        using var serverClient = await serverAcceptTask;
        byte[] payload = { 0xDE, 0xAD, 0xBE, 0xEF };

        await client.SendMessageAsync(payload);

        var stream = serverClient.GetStream();
        var buffer = new byte[16];
        int bytesRead = await stream
            .ReadAsync(buffer)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Multiple(() =>
        {
            Assert.That(bytesRead, Is.EqualTo(payload.Length));
            Assert.That(buffer[..bytesRead], Is.EqualTo(payload));
        });

        client.Disconnect();
    }

    [Test]
    public async Task MessageReceived_WhenServerWritesData_EventIsRaisedWithCorrectPayload()
    {
        var client = new TcpClientWrapper("127.0.0.1", _port);
        var serverAcceptTask = _server.AcceptTcpClientAsync();

        var tcs = new TaskCompletionSource<byte[]>();
        client.MessageReceived += (_, data) => tcs.TrySetResult(data);

        client.Connect();

        using var serverClient = await serverAcceptTask;
        byte[] toSend = [0xAA, 0xBB, 0xCC];
        await serverClient.GetStream().WriteAsync(toSend);

        var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.That(received, Is.EqualTo(toSend));

        client.Disconnect();
    }

    [Test]
    public async Task MessageReceived_MultipleChunks_AllChunksDelivered()
    {
        var client = new TcpClientWrapper("127.0.0.1", _port);
        var serverAcceptTask = _server.AcceptTcpClientAsync();

        var receivedChunks = new List<byte[]>();
        var tcs = new TaskCompletionSource<bool>();

        client.MessageReceived += (_, data) =>
        {
            lock (receivedChunks)
            {
                receivedChunks.Add(data);
                if (receivedChunks.Count >= 2)
                    tcs.TrySetResult(true);
            }
        };

        client.Connect();

        using var serverClient = await serverAcceptTask;
        var stream = serverClient.GetStream();

        await stream.WriteAsync(new byte[] { 0x01, 0x02 });
        await Task.Delay(50);
        await stream.WriteAsync(new byte[] { 0x03, 0x04 });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.That(receivedChunks, Is.Not.Empty);

        client.Disconnect();
    }
}
