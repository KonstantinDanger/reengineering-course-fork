using NetSdrClientApp.Networking;

namespace NetSdrClientApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var tcpClient = new TcpClientWrapper("127.0.0.1", 5000);
            var udpClient = new UdpClientWrapper(60000);

            App app = new(tcpClient, udpClient);

            app.Start();
        }
    }
}

