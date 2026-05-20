using System.Diagnostics.CodeAnalysis;

namespace NetSdrClientApp
{
    public class App
    {
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

        public App(NetSdrClient netSdrClient)
            => NetSdr = netSdrClient;

        [ExcludeFromCodeCoverage]
        public async Task Start()
        {
            Greet();
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

            if (key == ConsoleKey.Q)
                return false;

            await command(netSdr);

            return true;
        }

        public static void Greet() => Console.WriteLine("Program has started");

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

