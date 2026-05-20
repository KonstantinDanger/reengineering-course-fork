using NetSdrClientApp.Networking;
using System.Net;
using System.Net.Sockets;

namespace NetSdrClientAppTests;

public class UdpClientWrapperTests
{
    private static int GetFreeUdpPort()
    {
        using var probe = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        return ((IPEndPoint)probe.Client.LocalEndPoint!).Port;
    }

    [Test]
    public void StopListening_BeforeStart_DoesNotThrow()
    {
        var client = new UdpClientWrapper(GetFreeUdpPort());

        Assert.DoesNotThrow(client.StopListening);
    }

    [Test]
    public async Task StopListening_AfterStartListeningAsync_CompletesListenLoop()
    {
        int port = GetFreeUdpPort();
        var client = new UdpClientWrapper(port);

        var listenTask = client.StartListeningAsync();
        await Task.Delay(80);

        client.StopListening();

        await listenTask.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Pass("Listening loop stopped cleanly.");
    }

    [Test]
    public async Task StartListeningAsync_OnIncomingDatagram_RaisesMessageReceivedEvent()
    {
        int port = GetFreeUdpPort();
        var client = new UdpClientWrapper(port);

        var tcs = new TaskCompletionSource<byte[]>();
        client.MessageReceived += (_, data) => tcs.TrySetResult(data);

        var listenTask = client.StartListeningAsync();
        await Task.Delay(80);

        byte[] payload = [0x11, 0x22, 0x33];
        using var sender = new UdpClient();
        await sender.SendAsync(payload, new IPEndPoint(IPAddress.Loopback, port));

        var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.That(received, Is.EqualTo(payload));

        client.StopListening();
        await listenTask.WaitAsync(TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task StartListeningAsync_MultipleDatagrams_EventRaisedForEachDatagram()
    {
        int port = GetFreeUdpPort();
        var client = new UdpClientWrapper(port);

        var received = new List<byte[]>();
        var tcs = new TaskCompletionSource<bool>();

        client.MessageReceived += (_, data) =>
        {
            lock (received)
            {
                received.Add(data);
                if (received.Count >= 3)
                    tcs.TrySetResult(true);
            }
        };

        var listenTask = client.StartListeningAsync();
        await Task.Delay(80);

        using var sender = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);

        for (byte i = 1; i <= 3; i++)
            await sender.SendAsync(new byte[] { i }, endpoint);

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.That(received, Has.Count.EqualTo(3));
        Assert.That(received.Select(r => r[0]).Order(), Is.EqualTo(new byte[] { 1, 2, 3 }));

        client.StopListening();
        await listenTask.WaitAsync(TimeSpan.FromSeconds(3));
    }

    [Test]
    public void Exit_WhenNotStarted_DoesNotThrow()
    {
        var client = new UdpClientWrapper(GetFreeUdpPort());

        Assert.DoesNotThrow(client.Exit);
    }

    [Test]
    public async Task Exit_AfterStart_StopsListening()
    {
        int port = GetFreeUdpPort();
        var client = new UdpClientWrapper(port);

        var listenTask = client.StartListeningAsync();
        await Task.Delay(80);

        client.Exit();

        await listenTask.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Pass("Exit() stopped the listening loop.");
    }

    [Test]
    public void GetHashCode_SamePort_ReturnsSameValue()
    {
        const int port = 55_000;
        var a = new UdpClientWrapper(port);
        var b = new UdpClientWrapper(port);

        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    [Test]
    public void GetHashCode_DifferentPorts_ReturnDifferentValues()
    {
        var a = new UdpClientWrapper(55_001);
        var b = new UdpClientWrapper(55_002);

        Assert.That(a.GetHashCode(), Is.Not.EqualTo(b.GetHashCode()));
    }
}