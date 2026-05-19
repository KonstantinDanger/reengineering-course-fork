using System.Net;
using System.Net.Sockets;
using System.Reflection;
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

        int bytesRead = await stream.ReadAsync(buffer);

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

                int bytesRead = await stream.ReadAsync(buffer);

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

    [Test]
    public async Task HandleClientAsync_EchoesMessage_ClosesClient()
    {
        // Arrange
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        using var client = new TcpClient();

        Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();

        await client.ConnectAsync(IPAddress.Loopback, port);

        TcpClient serverClient = await acceptTask;

        using var cts = new CancellationTokenSource();

        // Start HandleClientAsync in background
        Task handlerTask = Program.HandleClientAsync(serverClient, cts.Token);

        using NetworkStream clientStream = client.GetStream();

        byte[] message = Encoding.UTF8.GetBytes("ping");
        byte[] buffer = new byte[1024];

        // Act
        await clientStream.WriteAsync(message, 0, message.Length);

        int bytesRead = await clientStream.ReadAsync(buffer);

        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        // Assert
        Assert.That(response, Is.EqualTo("ping"));

        // Close client stream to end server read loop
        client.Close();

        // Wait for handler to finish
        await handlerTask;

        Assert.That(serverClient.Connected, Is.False);

        // Cleanup
        listener.Stop();
    }

    [Test]
    public void Main_Method_Exists_And_IsAsync()
    {
        // Arrange
        MethodInfo? method = typeof(Program).GetMethod(
            "Main",
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        Assert.That(method, Is.Not.Null);

        Assert.That(
            method!.ReturnType,
            Is.EqualTo(typeof(Task)));
    }

    [Test]
    public async Task StartAsync_StopsWhenListenerDisposed()
    {
        // Arrange
        var server = new Program(5056);

        Task serverTask = Task.Run(server.StartAsync);

        await Task.Delay(TaskDelay);

        // Act
        server.Stop();

        // Assert
        await serverTask;

        Assert.That(serverTask.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public async Task HandleClientAsync_HandlesCancellation()
    {
        // Arrange
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        using var client = new TcpClient();

        Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();

        await client.ConnectAsync(IPAddress.Loopback, port);

        TcpClient serverClient = await acceptTask;

        using var cts = new CancellationTokenSource();

        Task handlerTask = Program.HandleClientAsync(serverClient, cts.Token);

        // Act
        cts.Cancel();

        // Assert
        Assert.DoesNotThrowAsync(async () => await handlerTask);

        listener.Stop();
    }

    [Test]
    public async Task RunAsync_StartsServerAndAcceptsConnection_BeforeCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        Task runTask = Program.RunAsync(cts.Token);

        await Task.Delay(TaskDelay);

        // Act
        using var client = new TcpClient();

        Assert.DoesNotThrowAsync(async () =>
            await client.ConnectAsync("127.0.0.1", 5000));

        cts.Cancel();
        await runTask;

        // Assert
        Assert.That(runTask.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public async Task RunAsync_CompletesCleanly_WhenCancelledAfterDelay()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        Task runTask = Program.RunAsync(cts.Token);

        await Task.Delay(TaskDelay);

        // Act
        cts.Cancel();
        await runTask;

        // Assert
        Assert.That(runTask.IsCompletedSuccessfully, Is.True);
        Assert.That(runTask.IsFaulted, Is.False);
    }

    [Test]
    public void RunAsync_WhenCancellationRequested_DoesNotThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        cts.CancelAfter(100);

        // Act + Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            await Program.RunAsync(cts.Token);
        });
    }
}
