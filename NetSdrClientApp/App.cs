using NetSdrClientApp.Networking;

namespace NetSdrClientApp
{
    public class App
    {
        private readonly TcpClientWrapper _tcpClient;
        private readonly UdpClientWrapper _udpClient;
        private readonly Dictionary<ConsoleKey, Func<NetSdrClient, Task>> _commands = new()
        {
            [ConsoleKey.C] = async client => await client.ConnectAsync(),
            [ConsoleKey.D] = async client => await Task.Run(client.Disconnect),
            [ConsoleKey.F] = async client => await client.ChangeFrequencyAsync(20000000, 1),
            [ConsoleKey.S] = async client =>
            {
                if (client.IQStarted)
                    await client.StopIQAsync();
                else
                    await client.StartIQAsync();
            },
        };

        public NetSdrClient NetSdr { get; private set; }

        public App(TcpClientWrapper tcpClient, UdpClientWrapper udpClient)
        {
            _tcpClient = tcpClient;
            _udpClient = udpClient;

            NetSdr = new(_tcpClient, _udpClient);
        }

        public async void Start()
        {
            Console.WriteLine("Program has started");

            ShowCommands();

            while (true)
            {
                var key = ReadKey();
                bool update = await PerformCommand(NetSdr, key);
                if (!update)
                    break;
            }
        }

        public async Task<bool> PerformCommand(NetSdrClient netSdr, ConsoleKey key)
        {
            if (!_commands.TryGetValue(key, out Func<NetSdrClient, Task>? command))
                return false;

            if (command == null || key == ConsoleKey.Q)
                return false;

            await command(netSdr);

            return true;
        }

        public static void ShowCommands()
        {
            Console.WriteLine(@"Usage:
            C - connect
            D - disconnect
            F - set frequency
            S - Start/Stop IQ listener
            Q - quit");
        }

        public static ConsoleKey ReadKey() => Console.ReadKey(intercept: true).Key;
    }
}

