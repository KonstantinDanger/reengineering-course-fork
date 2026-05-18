using System.Net.Sockets;
using System.Text;
using Program = EchoServer.Program;

namespace NetSdrClientAppTests;

[TestFixture]
public class EchoServerTests
{
    private const int TestPort = 5055;
    private const int TaskDelay = 200;

    [Test]
    public async Task StartAsync_ClientCanConnect()
    {
        // Arrange
        var server = new Program(TestPort);

        // Act
        _ = Task.Run(server.StartAsync);

        await Task.Delay(TaskDelay);

        using var client = new TcpClient();

        // Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            await client.ConnectAsync("127.0.0.1", TestPort);
        });

        server.Stop();
    }

    [Test]
    public async Task Server_EchoesBack_Message()
    {
        // Arrange
        var server = new Program(TestPort);

        _ = Task.Run(server.StartAsync);

        await Task.Delay(TaskDelay);

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", TestPort);

        using NetworkStream stream = client.GetStream();

        byte[] message = Encoding.UTF8.GetBytes("message");
        byte[] buffer = new byte[1024];

        // Act
        await stream.WriteAsync(message, 0, message.Length);

        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        // Assert
        Assert.That(response, Is.EqualTo("message"));

        server.Stop();
    }

    [Test]
    public async Task Stop_ServerStopsAcceptingConnections()
    {
        // Arrange
        var server = new Program(TestPort);

        _ = Task.Run(server.StartAsync);

        await Task.Delay(TaskDelay);

        server.Stop();

        await Task.Delay(TaskDelay);

        using var client = new TcpClient();

        // Act and Assert
        Assert.ThrowsAsync<SocketException>(async () =>
        {
            await client.ConnectAsync("127.0.0.1", TestPort);
        });
    }

    [Test]
    public async Task Server_CanHandle_MultipleClients()
    {
        // Arrange
        var server = new Program(TestPort);

        _ = Task.Run(server.StartAsync);

        await Task.Delay(TaskDelay);

        // Act
        var clients = Enumerable.Range(0, 5)
            .Select(async i =>
            {
                using var client = new TcpClient();

                await client.ConnectAsync("127.0.0.1", TestPort);

                using var stream = client.GetStream();

                string text = $"client-{i}";
                byte[] message = Encoding.UTF8.GetBytes(text);

                await stream.WriteAsync(message, 0, message.Length);

                byte[] buffer = new byte[1024];

                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            });

        string[] responses = await Task.WhenAll(clients);

        // Assert
        for (int i = 0; i < responses.Length; i++)
        {
            Assert.That(responses[i], Is.EqualTo($"client-{i}"));
        }

        server.Stop();
    }
}
