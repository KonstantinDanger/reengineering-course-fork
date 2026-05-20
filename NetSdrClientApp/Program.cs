using NetSdrClientApp.Networking;
using System.Diagnostics.CodeAnalysis;

namespace NetSdrClientApp
{
    public class Program
    {
        [ExcludeFromCodeCoverage]
        public static async Task Main(string[] args)
        {
            var tcpClient = new TcpClientWrapper("127.0.0.1", 5000);
            var udpClient = new UdpClientWrapper(60000);
            var client = new NetSdrClient(tcpClient, udpClient);

            App app = new(client);

            await app.Start();
        }
    }
}

