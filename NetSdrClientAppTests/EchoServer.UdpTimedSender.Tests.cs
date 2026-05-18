using EchoServer;
using System.Net.Sockets;

namespace NetSdrClientAppTests;

public class UdpTimedSenderTests
{
    private const string TestHost = "127.0.0.1";
    private const int TestPort = 60001;
    private UdpClient _receiver;

    [SetUp]
    public void SetUp() => _receiver = new UdpClient(TestPort);

    [TearDown]
    public void TearDown() => _receiver.Dispose();

    [Test]
    public async Task Sender_ShouldSendPacketWithCorrectHeaderAndLength()
    {
        // Arrange
        using var sender = new UdpTimedSender(TestHost, TestPort);

        // Act
        sender.StartSending(1000);

        var receiveTask = _receiver.ReceiveAsync();
        if (await Task.WhenAny(receiveTask, Task.Delay(2000)) == receiveTask)
        {
            var result = receiveTask.Result;

            // Assert
            Assert.That(result.Buffer.Length, Is.EqualTo(1028));
            Assert.That(result.Buffer[0], Is.EqualTo(0x04));
            Assert.That(result.Buffer[1], Is.EqualTo(0x84));
        }
        else
        {
            Assert.Fail("Timed out waiting for UDP packet.");
        }
    }

    [Test]
    public void StartSending_CalledTwice_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var sender = new UdpTimedSender(TestHost, TestPort);
        sender.StartSending(1000);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => sender.StartSending(1000));

        // Assert
        Assert.That(ex.Message, Is.EqualTo("Sender is already running."));
    }

    [Test]
    public async Task StopSending_ShouldPreventFurtherMessagesFromBeingSent()
    {
        // Arrange
        using var sender = new UdpTimedSender(TestHost, TestPort);

        // Act
        sender.StartSending(100);

        await _receiver.ReceiveAsync();

        sender.StopSending();

        await Task.Delay(300);

        // Assert
        Assert.That(_receiver.Available, Is.EqualTo(0));
    }
}
