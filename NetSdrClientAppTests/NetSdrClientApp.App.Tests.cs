using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class AppTests
{
    private static App CreateApp()
    {
        var tcp = new TcpClientWrapper("127.0.0.1", 5000);
        var udp = new UdpClientWrapper(60000);
        var netSdrClient = new NetSdrClient(tcp, udp);
        return new App(netSdrClient);
    }

    [Test]
    public void ShowCommands_ThrowsNoExceptions()
        => Assert.DoesNotThrow(App.ShowCommands);

    [Test]
    public void Greet_ThrowsNoExceptions()
        => Assert.DoesNotThrow(App.Greet);

    [Test]
    public async Task PerformCommand_ReceiveCorrectKey_ReturnTrue()
    {
        App app = CreateApp();
        ConsoleKey key = ConsoleKey.C;

        bool result = await app.PerformCommand(app.NetSdr, key);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task PerformCommand_ReceiveIncorrectKey_ReturnFalse()
    {
        App app = CreateApp();
        ConsoleKey key = ConsoleKey.DownArrow;

        bool result = await app.PerformCommand(app.NetSdr, key);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task PerformCommand_ReceiveQuitKey_ReturnFalse()
    {
        App app = CreateApp();
        ConsoleKey key = ConsoleKey.Q;

        bool result = await app.PerformCommand(app.NetSdr, key);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task PerformCommand_ReceiveDKey_ReturnTrue()
    {
        App app = CreateApp();
        ConsoleKey key = ConsoleKey.D;

        bool result = await app.PerformCommand(app.NetSdr, key);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task PerformCommand_ReceiveFKey_ReturnTrue()
    {
        App app = CreateApp();
        ConsoleKey key = ConsoleKey.F;

        bool result = await app.PerformCommand(app.NetSdr, key);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task PerformCommand_ReceiveSKey_ReturnTrue()
    {
        App app = CreateApp();
        ConsoleKey key = ConsoleKey.S;

        bool result = await app.PerformCommand(app.NetSdr, key);

        Assert.That(result, Is.True);
    }
}