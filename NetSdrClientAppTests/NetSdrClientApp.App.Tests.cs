using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class AppTests
{
    private static App CreateApp()
    {
        var tcp = new TcpClientWrapper("127.0.0.1", 5000);
        var udp = new UdpClientWrapper(60000);
        return new App(tcp, udp);
    }

    [Test]
    public void ShowCommands_ThrowsNoExceptions()
        => Assert.DoesNotThrow(App.ShowCommands);

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
}